using MediatR;
using Microsoft.AspNetCore.Mvc;
using SixSeven.Application.Features.Reports;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Controllers;

[ApiController]
[Route("reports")]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("volunteers")]
    public async Task<IActionResult> GetVolunteerActivityReport(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var fmt = ParseFormat(format);

        FileReportResult file = await mediator.Send(
            new GetVolunteerActivityReportFileQuery(fromUtc, toUtc, fmt),
            cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEventParticipationReport(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var fmt = ParseFormat(format);

        FileReportResult file = await mediator.Send(
            new GetEventAssignmentsReportFileQuery(fromUtc, toUtc, fmt),
            cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    private static ReportFileFormat ParseFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return ReportFileFormat.Csv;

        return format.Trim().ToLowerInvariant() switch
        {
            "pdf" => ReportFileFormat.Pdf,
            "csv" => ReportFileFormat.Csv,
            _ => ReportFileFormat.Csv
        };
    }
}