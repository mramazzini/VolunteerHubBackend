using Moq;
using SixSeven.Application.Features.Reports;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Application.Tests.Features.Reports
{
    [TestFixture]
    public class GetVolunteerActivityReportFileQueryHandlerTests
    {
        private Mock<IVolunteerReportingRepository> _reportingRepo = null!;
        private Mock<IFileReportService> _fileReportService = null!;
        private GetVolunteerActivityReportFileQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _reportingRepo = new Mock<IVolunteerReportingRepository>(MockBehavior.Strict);
            _fileReportService = new Mock<IFileReportService>(MockBehavior.Strict);
            _handler = new GetVolunteerActivityReportFileQueryHandler(_reportingRepo.Object, _fileReportService.Object);
        }

        private static Event CreateEvent(
            string id,
            string name,
            DateTime dateUtc,
            string location,
            EventUrgency urgency)
        {
            var ev = new Event(
                name,
                "Description",
                location,
                dateUtc,
                urgency,
                new List<VolunteerSkill>());

            typeof(Event).GetProperty(nameof(Event.Id))!
                .SetValue(ev, id);

            return ev;
        }

        private static UserCredentials CreateUser(string id, string email)
        {
            var user = new UserCredentials(email, "hash", UserRole.Volunteer);
            typeof(UserCredentials).GetProperty(nameof(UserCredentials.Id))!
                .SetValue(user, id);
            return user;
        }

        private static void AttachProfile(
            UserCredentials user,
            string firstName,
            string lastName)
        {
            var profile = new UserProfile(
                userCredentialsId: user.Id,
                firstName: firstName,
                lastName: lastName,
                addressOne: "123 Main",
                city: "City",
                state: "TX",
                zipCode: "77001",
                preferences: null,
                skills: new List<VolunteerSkill>(),
                otherSkills: null,
                availability: new List<string>());

            typeof(UserProfile).GetProperty(nameof(UserProfile.Credentials))!
                .SetValue(profile, user);

            typeof(UserCredentials).GetProperty(nameof(UserCredentials.Profile))!
                .SetValue(user, profile);
        }

        private static Domain.Entities.VolunteerHistory CreateHistory(
            string id,
            UserCredentials user,
            Event ev,
            DateTime dateUtc,
            int durationMinutes)
        {
            var h = new Domain.Entities.VolunteerHistory(
                userId: user.Id,
                eventId: ev.Id,
                dateUtc: dateUtc,
                durationMinutes: durationMinutes);

            typeof(Domain.Entities.VolunteerHistory).GetProperty(nameof(Domain.Entities.VolunteerHistory.Id))!
                .SetValue(h, id);

            typeof(Domain.Entities.VolunteerHistory).GetProperty(nameof(Domain.Entities.VolunteerHistory.User))!
                .SetValue(h, user);

            typeof(Domain.Entities.VolunteerHistory).GetProperty(nameof(Domain.Entities.VolunteerHistory.Event))!
                .SetValue(h, ev);

            return h;
        }

        [Test]
        public async Task Handle_MapsHistoriesToRows_OrdersThem_AndCallsFileService()
        {
            var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

            var ev1 = CreateEvent(
                id: "event-1",
                name: "Alpha Event",
                dateUtc: new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                location: "Hall A",
                urgency: EventUrgency.Low);

            var ev2 = CreateEvent(
                id: "event-2",
                name: "Beta Event",
                dateUtc: new DateTime(2025, 1, 5, 14, 0, 0, DateTimeKind.Utc),
                location: "Hall B",
                urgency: EventUrgency.Medium);

            var user1 = CreateUser("user-1", "alice@test.com");
            var user2 = CreateUser("user-2", "bob@test.com");
            var user3 = CreateUser("user-3", "charlie@test.com");

            AttachProfile(user1, "Alice", "Smith");
            AttachProfile(user2, "Bob", "Jones");
            AttachProfile(user3, "Aaron", "Brown");

            var h1 = CreateHistory(
                id: "h1",
                user: user1,
                ev: ev1,
                dateUtc: new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                durationMinutes: 60);

            var h2 = CreateHistory(
                id: "h2",
                user: user2,
                ev: ev1,
                dateUtc: new DateTime(2025, 1, 10, 11, 0, 0, DateTimeKind.Utc),
                durationMinutes: 90);

            var h3 = CreateHistory(
                id: "h3",
                user: user3,
                ev: ev2,
                dateUtc: new DateTime(2025, 1, 5, 15, 0, 0, DateTimeKind.Utc),
                durationMinutes: 45);

            var histories = new List<Domain.Entities.VolunteerHistory> { h1, h2, h3 };

            _reportingRepo
                .Setup(r => r.GetVolunteerActivityAsync(from, to, It.IsAny<CancellationToken>()))
                .ReturnsAsync(histories);

            IReadOnlyList<VolunteerActivityRowDto>? capturedRows = null;

            var expectedResult = new FileReportResult(
                FileName: "vol-activity.csv",
                ContentType: "text/csv",
                Content: new byte[] { 1, 2, 3 });

            _fileReportService
                .Setup(s => s.GenerateVolunteerActivityReportAsync(
                    It.IsAny<IReadOnlyList<VolunteerActivityRowDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<VolunteerActivityRowDto>, ReportFileFormat, CancellationToken>((rows, _, _) =>
                {
                    capturedRows = rows;
                })
                .ReturnsAsync(expectedResult);

            var query = new GetVolunteerActivityReportFileQuery(
                FromUtc: from,
                ToUtc: to,
                Format: ReportFileFormat.Csv);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.SameAs(expectedResult));
            Assert.That(capturedRows, Is.Not.Null);
            Assert.That(capturedRows!.Count, Is.EqualTo(3));

            var ordered = capturedRows.ToList();

            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].EventId, Is.EqualTo(ev2.Id));
                Assert.That(ordered[0].EventName, Is.EqualTo(ev2.Name));
                Assert.That(ordered[0].UserId, Is.EqualTo(user3.Id));
                Assert.That(ordered[0].FullName, Is.EqualTo("Aaron Brown"));
                Assert.That(ordered[0].Email, Is.EqualTo(user3.Email));
                Assert.That(ordered[0].EventDateUtc, Is.EqualTo(ev2.DateUtc));
                Assert.That(ordered[0].DurationMinutes, Is.EqualTo(h3.DurationMinutes));

                Assert.That(ordered[1].EventId, Is.EqualTo(ev1.Id));
                Assert.That(ordered[1].EventName, Is.EqualTo(ev1.Name));
                Assert.That(ordered[1].UserId, Is.EqualTo(user1.Id));
                Assert.That(ordered[1].FullName, Is.EqualTo("Alice Smith"));
                Assert.That(ordered[1].Email, Is.EqualTo(user1.Email));
                Assert.That(ordered[1].EventDateUtc, Is.EqualTo(ev1.DateUtc));
                Assert.That(ordered[1].DurationMinutes, Is.EqualTo(h1.DurationMinutes));

                Assert.That(ordered[2].EventId, Is.EqualTo(ev1.Id));
                Assert.That(ordered[2].EventName, Is.EqualTo(ev1.Name));
                Assert.That(ordered[2].UserId, Is.EqualTo(user2.Id));
                Assert.That(ordered[2].FullName, Is.EqualTo("Bob Jones"));
                Assert.That(ordered[2].Email, Is.EqualTo(user2.Email));
                Assert.That(ordered[2].EventDateUtc, Is.EqualTo(ev1.DateUtc));
                Assert.That(ordered[2].DurationMinutes, Is.EqualTo(h2.DurationMinutes));
            });

            _reportingRepo.Verify(
                r => r.GetVolunteerActivityAsync(from, to, It.IsAny<CancellationToken>()),
                Times.Once);

            _fileReportService.Verify(
                s => s.GenerateVolunteerActivityReportAsync(
                    It.IsAny<IReadOnlyList<VolunteerActivityRowDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_NoHistories_PassesEmptyRowsToFileService()
        {
            _reportingRepo
                .Setup(r => r.GetVolunteerActivityAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.VolunteerHistory>());

            IReadOnlyList<VolunteerActivityRowDto>? capturedRows = null;

            var expectedResult = new FileReportResult(
                FileName: "empty.csv",
                ContentType: "text/csv",
                Content: Array.Empty<byte>());

            _fileReportService
                .Setup(s => s.GenerateVolunteerActivityReportAsync(
                    It.IsAny<IReadOnlyList<VolunteerActivityRowDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<VolunteerActivityRowDto>, ReportFileFormat, CancellationToken>((rows, _, _) =>
                {
                    capturedRows = rows;
                })
                .ReturnsAsync(expectedResult);

            var query = new GetVolunteerActivityReportFileQuery(
                FromUtc: null,
                ToUtc: null,
                Format: ReportFileFormat.Csv);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.SameAs(expectedResult));
            Assert.That(capturedRows, Is.Not.Null);
            Assert.That(capturedRows, Is.Empty);

            _reportingRepo.Verify(
                r => r.GetVolunteerActivityAsync(null, null, It.IsAny<CancellationToken>()),
                Times.Once);

            _fileReportService.Verify(
                s => s.GenerateVolunteerActivityReportAsync(
                    It.IsAny<IReadOnlyList<VolunteerActivityRowDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
