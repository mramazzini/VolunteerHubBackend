using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Features.Reports;
using SixSeven.Controllers;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.API.Tests.Controllers
{
    [TestFixture]
    public class ReportsControllerTests
    {
        private Mock<IMediator> _mediator = null!;
        private ReportsController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<IMediator>();
            _controller = new ReportsController(_mediator.Object);
        }

        [Test]
        public async Task GetVolunteerActivityReport_DefaultFormat_UsesCsv()
        {
            var file = new FileReportResult(
                FileName: "volunteers.csv",
                ContentType: "text/csv",
                Content: new byte[] { 1, 2, 3 });

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetVolunteerActivityReportFileQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);

            var result = await _controller.GetVolunteerActivityReport(
                fromUtc: null,
                toUtc: null,
                format: "csv",
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<FileContentResult>());
            var fileResult = (FileContentResult)result;
            Assert.That(fileResult.ContentType, Is.EqualTo("text/csv"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("volunteers.csv"));
            Assert.That(fileResult.FileContents, Is.EqualTo(file.Content));

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetVolunteerActivityReportFileQuery>(q =>
                        q.Format == ReportFileFormat.Csv),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetVolunteerActivityReport_PdfFormat_UsesPdf()
        {
            var file = new FileReportResult(
                FileName: "volunteers.pdf",
                ContentType: "application/pdf",
                Content: new byte[] { 9, 9, 9 });

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetVolunteerActivityReportFileQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);

            var result = await _controller.GetVolunteerActivityReport(
                fromUtc: null,
                toUtc: null,
                format: "pdf",
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<FileContentResult>());
            var fileResult = (FileContentResult)result;
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("volunteers.pdf"));

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetVolunteerActivityReportFileQuery>(q =>
                        q.Format == ReportFileFormat.Pdf),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetEventParticipationReport_DefaultFormat_UsesCsv()
        {
            var file = new FileReportResult(
                FileName: "events.csv",
                ContentType: "text/csv",
                Content: new byte[] { 4, 5, 6 });

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetEventAssignmentsReportFileQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);

            var result = await _controller.GetEventParticipationReport(
                fromUtc: null,
                toUtc: null,
                format: "csv",
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<FileContentResult>());
            var fileResult = (FileContentResult)result;
            Assert.That(fileResult.ContentType, Is.EqualTo("text/csv"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("events.csv"));

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetEventAssignmentsReportFileQuery>(q =>
                        q.Format == ReportFileFormat.Csv),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetEventParticipationReport_InvalidFormat_FallsBackToCsv()
        {
            var file = new FileReportResult(
                FileName: "events.csv",
                ContentType: "text/csv",
                Content: new byte[] { 7, 8, 9 });

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetEventAssignmentsReportFileQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(file);

            var result = await _controller.GetEventParticipationReport(
                fromUtc: null,
                toUtc: null,
                format: "not-a-format",
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<FileContentResult>());

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetEventAssignmentsReportFileQuery>(q =>
                        q.Format == ReportFileFormat.Csv),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
