using System.Text;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Enums;

namespace SixSeven.Infrastructure.Tests
{
    [TestFixture]
    public class FileReportServiceTests
    {
        private FileReportService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new FileReportService();
        }

        [Test]
        public async Task GenerateVolunteerActivityReportAsync_Csv_ProducesExpectedHeaderAndRow()
        {
            var rows = new List<VolunteerActivityRowDto>
            {
                new VolunteerActivityRowDto(
                    "user-1",
                    "Test \"User\"",
                    "test@example.com",
                    "event-1",
                    "Food Drive, Downtown",
                    new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    90)
            };

            var result = await _service.GenerateVolunteerActivityReportAsync(
                rows,
                ReportFileFormat.Csv);

            Assert.That(result.ContentType, Is.EqualTo("text/csv"));
            Assert.That(result.FileName, Does.StartWith("volunteer-activity-"));
            Assert.That(result.FileName, Does.EndWith(".csv"));

            var csv = Encoding.UTF8.GetString(result.Content);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0].Trim(), Is.EqualTo("UserId,FullName,Email,EventId,EventName,EventDateUtc,DurationMinutes"));
            Assert.That(lines[1], Does.Contain("\"user-1\""));
            Assert.That(lines[1], Does.Contain("\"Test \"\"User\"\"\""));
            Assert.That(lines[1], Does.Contain("\"Food Drive, Downtown\""));
        }

        [Test]
        public async Task GenerateVolunteerActivityReportAsync_Csv_HandlesEmptyRows()
        {
            IReadOnlyList<VolunteerActivityRowDto> rows = Array.Empty<VolunteerActivityRowDto>();

            var result = await _service.GenerateVolunteerActivityReportAsync(
                rows,
                ReportFileFormat.Csv);

            var csv = Encoding.UTF8.GetString(result.Content);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.That(lines.Length, Is.EqualTo(1));
            Assert.That(lines[0].Trim(), Is.EqualTo("UserId,FullName,Email,EventId,EventName,EventDateUtc,DurationMinutes"));
        }

        [Test]
        public async Task GenerateVolunteerActivityReportAsync_Pdf_ReturnsPdfFile()
        {
            var rows = new List<VolunteerActivityRowDto>
            {
                new VolunteerActivityRowDto(
                    "user-1",
                    "Test User",
                    "test@example.com",
                    "event-1",
                    "Food Drive",
                    new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    90)
            };

            var result = await _service.GenerateVolunteerActivityReportAsync(
                rows,
                ReportFileFormat.Pdf);

            Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result.FileName, Does.StartWith("volunteer-activity-"));
            Assert.That(result.FileName, Does.EndWith(".pdf"));
            Assert.That(result.Content.Length, Is.GreaterThan(0));
        }

        [Test]
        public void GenerateVolunteerActivityReportAsync_UnsupportedFormat_Throws()
        {
            IReadOnlyList<VolunteerActivityRowDto> rows = Array.Empty<VolunteerActivityRowDto>();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await _service.GenerateVolunteerActivityReportAsync(
                    rows,
                    (ReportFileFormat)999));
        }

        [Test]
        public async Task GenerateEventAssignmentsReportAsync_Csv_HeaderOnlyForEmptyList()
        {
            IReadOnlyList<EventAssignmentReportDto> reports = Array.Empty<EventAssignmentReportDto>();

            var result = await _service.GenerateEventAssignmentsReportAsync(
                reports,
                ReportFileFormat.Csv);

            Assert.That(result.ContentType, Is.EqualTo("text/csv"));
            Assert.That(result.FileName, Does.StartWith("event-assignments-"));
            Assert.That(result.FileName, Does.EndWith(".csv"));

            var csv = Encoding.UTF8.GetString(result.Content);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.That(lines.Length, Is.EqualTo(1));
            Assert.That(
                lines[0].Trim(),
                Is.EqualTo("EventId,EventName,EventDateUtc,Location,Urgency,RequiredSkills,UserId,FullName,Email,ParticipationDateUtc,DurationMinutes"));
        }

        [Test]
        public async Task GenerateEventAssignmentsReportAsync_Pdf_ReturnsPdfFile()
        {
            IReadOnlyList<EventAssignmentReportDto> reports = Array.Empty<EventAssignmentReportDto>();

            var result = await _service.GenerateEventAssignmentsReportAsync(
                reports,
                ReportFileFormat.Pdf);

            Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result.FileName, Does.StartWith("event-assignments-"));
            Assert.That(result.FileName, Does.EndWith(".pdf"));
            Assert.That(result.Content.Length, Is.GreaterThan(0));
        }

        [Test]
        public void GenerateEventAssignmentsReportAsync_UnsupportedFormat_Throws()
        {
            IReadOnlyList<EventAssignmentReportDto> reports = Array.Empty<EventAssignmentReportDto>();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await _service.GenerateEventAssignmentsReportAsync(
                    reports,
                    (ReportFileFormat)999));
        }
    }
}
