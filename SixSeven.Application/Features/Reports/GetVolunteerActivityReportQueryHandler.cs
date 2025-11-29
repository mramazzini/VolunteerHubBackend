using MediatR;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Application.Features.Reports;

public sealed record GetVolunteerActivityReportFileQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    ReportFileFormat Format
) : IRequest<FileReportResult>;

public sealed class GetVolunteerActivityReportFileQueryHandler(
    IVolunteerReportingRepository reportingRepository,
    IFileReportService fileReportService)
    : IRequestHandler<GetVolunteerActivityReportFileQuery, FileReportResult>
{
    public async Task<FileReportResult> Handle(
        GetVolunteerActivityReportFileQuery request,
        CancellationToken cancellationToken)
    {
        var histories = await reportingRepository.GetVolunteerActivityAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        var rows = histories
            .Select(h => new VolunteerActivityRowDto(
                UserId: h.UserId,
                FullName: BuildFullName(h.User.Profile),
                Email:   h.User.Email,
                EventId: h.EventId,
                EventName: h.Event.Name,
                EventDateUtc: h.Event.DateUtc,
                DurationMinutes: h.DurationMinutes
            ))
            .OrderBy(r => r.EventDateUtc)
            .ThenBy(r => r.EventName)
            .ThenBy(r => r.FullName)
            .ToList();

        return await fileReportService.GenerateVolunteerActivityReportAsync(
            rows,
            request.Format,
            cancellationToken);
    }

    private static string BuildFullName(UserProfile? profile)
    {
        if (profile is null)
            return "No name given";

        var first = profile.FirstName?.Trim();
        var last = profile.LastName?.Trim();

        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            return "No name given";

        if (string.IsNullOrWhiteSpace(first))
            return last!;

        if (string.IsNullOrWhiteSpace(last))
            return first!;

        return $"{first} {last}";
    }
}
