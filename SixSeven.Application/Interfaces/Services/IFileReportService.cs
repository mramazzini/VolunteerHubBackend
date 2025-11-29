using SixSeven.Domain.DTO;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Application.Interfaces.Services;

public interface IFileReportService
{
    Task<FileReportResult> GenerateVolunteerActivityReportAsync(
        IReadOnlyList<VolunteerActivityRowDto> rows,
        ReportFileFormat format,
        CancellationToken cancellationToken = default);

    Task<FileReportResult> GenerateEventAssignmentsReportAsync(
        IReadOnlyList<EventAssignmentReportDto> reports,
        ReportFileFormat format,
        CancellationToken cancellationToken = default);
}
