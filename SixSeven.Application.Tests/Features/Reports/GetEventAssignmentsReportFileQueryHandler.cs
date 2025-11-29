using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
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
    public class GetEventAssignmentsReportFileQueryHandlerTests
    {
        private Mock<IVolunteerReportingRepository> _reportingRepo = null!;
        private Mock<IFileReportService> _fileReportService = null!;
        private GetEventAssignmentsReportFileQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _reportingRepo = new Mock<IVolunteerReportingRepository>(MockBehavior.Strict);
            _fileReportService = new Mock<IFileReportService>(MockBehavior.Strict);
            _handler = new GetEventAssignmentsReportFileQueryHandler(_reportingRepo.Object, _fileReportService.Object);
        }

        private static Event CreateEvent(
            string id,
            string name,
            DateTime dateUtc,
            string location,
            EventUrgency urgency,
            IEnumerable<VolunteerSkill> skills)
        {
            var ev = new Event(
                name,
                "Description",
                location,
                dateUtc,
                urgency,
                skills);

            typeof(Event).GetProperty(nameof(Event.Id))!
                .SetValue(ev, id);

            return ev;
        }

        private static UserCredentials CreateUser(string id, string email, UserRole role)
        {
            var user = new UserCredentials(email, "hash", role);
            typeof(UserCredentials).GetProperty(nameof(UserCredentials.Id))!
                .SetValue(user, id);
            return user;
        }

        private static UserProfile CreateProfile(
            UserCredentials user,
            string? firstName,
            string? lastName)
        {
            var profile = new UserProfile(
                userCredentialsId: user.Id,
                firstName: firstName ?? "",
                lastName: lastName ?? "",
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

            return profile;
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
        public async Task Handle_BuildsGroupedEventDtos_AndDelegatesToFileReportService()
        {
            var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

            var ev1 = CreateEvent(
                id: "event-1",
                name: "Earlier Event",
                dateUtc: new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                location: "Hall A",
                urgency: EventUrgency.Low,
                skills: new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving });

            var ev2 = CreateEvent(
                id: "event-2",
                name: "Later Event",
                dateUtc: new DateTime(2025, 1, 20, 15, 0, 0, DateTimeKind.Utc),
                location: "Hall B",
                urgency: EventUrgency.High,
                skills: new[] { VolunteerSkill.Teaching });

            var user1 = CreateUser("user-1", "alice@test.com", UserRole.Volunteer);
            var user2 = CreateUser("user-2", "bob@test.com", UserRole.Volunteer);
            var user3 = CreateUser("user-3", "no-profile@test.com", UserRole.Volunteer);

            CreateProfile(user1, "Alice", "Smith");
            CreateProfile(user2, "Bob", "Jones");

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
                dateUtc: new DateTime(2025, 1, 20, 15, 30, 0, DateTimeKind.Utc),
                durationMinutes: 45);

            var histories = new List<Domain.Entities.VolunteerHistory> { h2, h3, h1 };

            _reportingRepo
                .Setup(r => r.GetVolunteerActivityAsync(from, to, It.IsAny<CancellationToken>()))
                .ReturnsAsync(histories);

            IReadOnlyList<EventAssignmentReportDto>? capturedReports = null;

            var expectedResult = new FileReportResult(
                FileName: "dummy.csv",
                ContentType: "text/csv",
                Content: new byte[] { 1, 2, 3 });

            _fileReportService
                .Setup(s => s.GenerateEventAssignmentsReportAsync(
                    It.IsAny<IReadOnlyList<EventAssignmentReportDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<EventAssignmentReportDto>, ReportFileFormat, CancellationToken>((reports, _, _) =>
                {
                    capturedReports = reports;
                })
                .ReturnsAsync(expectedResult);

            var query = new GetEventAssignmentsReportFileQuery(
                FromUtc: from,
                ToUtc: to,
                Format: ReportFileFormat.Csv);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.SameAs(expectedResult));
            Assert.That(capturedReports, Is.Not.Null);
            Assert.That(capturedReports!.Count, Is.EqualTo(2));

            var firstEventDto = capturedReports[0];
            var secondEventDto = capturedReports[1];

            Assert.Multiple(() =>
            {
                Assert.That(firstEventDto.EventId, Is.EqualTo(ev1.Id));
                Assert.That(firstEventDto.EventName, Is.EqualTo(ev1.Name));
                Assert.That(firstEventDto.EventDateUtc, Is.EqualTo(ev1.DateUtc));
                Assert.That(firstEventDto.Location, Is.EqualTo(ev1.Location));
                Assert.That(firstEventDto.Urgency, Is.EqualTo(ev1.Urgency.ToString()));
                Assert.That(firstEventDto.RequiredSkills, Is.EqualTo(
                    ev1.RequiredSkills.Select(s => s.ToString()).OrderBy(s => s).ToList()));
                Assert.That(firstEventDto.Volunteers.Count, Is.EqualTo(2));

                Assert.That(secondEventDto.EventId, Is.EqualTo(ev2.Id));
                Assert.That(secondEventDto.EventName, Is.EqualTo(ev2.Name));
                Assert.That(secondEventDto.EventDateUtc, Is.EqualTo(ev2.DateUtc));
                Assert.That(secondEventDto.Location, Is.EqualTo(ev2.Location));
                Assert.That(secondEventDto.Urgency, Is.EqualTo(ev2.Urgency.ToString()));
                Assert.That(secondEventDto.RequiredSkills, Is.EqualTo(
                    ev2.RequiredSkills.Select(s => s.ToString()).OrderBy(s => s).ToList()));
                Assert.That(secondEventDto.Volunteers.Count, Is.EqualTo(1));
            });

            var v1 = firstEventDto.Volunteers[0];
            var v2 = firstEventDto.Volunteers[1];

            Assert.Multiple(() =>
            {
                Assert.That(v1.UserId, Is.EqualTo(user1.Id));
                Assert.That(v1.FullName, Is.EqualTo("Alice Smith"));
                Assert.That(v1.Email, Is.EqualTo(user1.Email));
                Assert.That(v1.ParticipationDateUtc, Is.EqualTo(h1.DateUtc));
                Assert.That(v1.DurationMinutes, Is.EqualTo(h1.DurationMinutes));

                Assert.That(v2.UserId, Is.EqualTo(user2.Id));
                Assert.That(v2.FullName, Is.EqualTo("Bob Jones"));
                Assert.That(v2.Email, Is.EqualTo(user2.Email));
                Assert.That(v2.ParticipationDateUtc, Is.EqualTo(h2.DateUtc));
                Assert.That(v2.DurationMinutes, Is.EqualTo(h2.DurationMinutes));
            });

            var v3 = secondEventDto.Volunteers[0];
            Assert.Multiple(() =>
            {
                Assert.That(v3.UserId, Is.EqualTo(user3.Id));
                Assert.That(v3.Email, Is.EqualTo(user3.Email));
                Assert.That(v3.ParticipationDateUtc, Is.EqualTo(h3.DateUtc));
                Assert.That(v3.DurationMinutes, Is.EqualTo(h3.DurationMinutes));
            });

            _reportingRepo.Verify(
                r => r.GetVolunteerActivityAsync(from, to, It.IsAny<CancellationToken>()),
                Times.Once);

            _fileReportService.Verify(
                s => s.GenerateEventAssignmentsReportAsync(
                    It.IsAny<IReadOnlyList<EventAssignmentReportDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_NoHistories_PassesEmptyListToFileService()
        {
            _reportingRepo
                .Setup(r => r.GetVolunteerActivityAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.VolunteerHistory>());

            IReadOnlyList<EventAssignmentReportDto>? capturedReports = null;

            var expectedResult = new FileReportResult(
                FileName: "empty.csv",
                ContentType: "text/csv",
                Content: Array.Empty<byte>());

            _fileReportService
                .Setup(s => s.GenerateEventAssignmentsReportAsync(
                    It.IsAny<IReadOnlyList<EventAssignmentReportDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<EventAssignmentReportDto>, ReportFileFormat, CancellationToken>((reports, _, _) =>
                {
                    capturedReports = reports;
                })
                .ReturnsAsync(expectedResult);

            var query = new GetEventAssignmentsReportFileQuery(
                FromUtc: null,
                ToUtc: null,
                Format: ReportFileFormat.Csv);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.SameAs(expectedResult));
            Assert.That(capturedReports, Is.Not.Null);
            Assert.That(capturedReports, Is.Empty);

            _reportingRepo.Verify(
                r => r.GetVolunteerActivityAsync(null, null, It.IsAny<CancellationToken>()),
                Times.Once);

            _fileReportService.Verify(
                s => s.GenerateEventAssignmentsReportAsync(
                    It.IsAny<IReadOnlyList<EventAssignmentReportDto>>(),
                    ReportFileFormat.Csv,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
