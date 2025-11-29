using MediatR;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Application.Features.Reports;

public sealed record GetEventAssignmentsReportFileQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    ReportFileFormat Format
) : IRequest<FileReportResult>;

public sealed class GetEventAssignmentsReportFileQueryHandler(
    IVolunteerReportingRepository reportingRepository,
    IFileReportService fileReportService)
    : IRequestHandler<GetEventAssignmentsReportFileQuery, FileReportResult>
{
    public async Task<FileReportResult> Handle(
        GetEventAssignmentsReportFileQuery request,
        CancellationToken cancellationToken)
    {
        var histories = await reportingRepository.GetVolunteerActivityAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        var grouped = histories
            .GroupBy(h => h.Event)
            .OrderBy(g => g.Key.DateUtc)
            .ThenBy(g => g.Key.Name);

        var reports = new List<EventAssignmentReportDto>();

        foreach (var group in grouped)
        {
            var ev = group.Key;

            var volunteers = group
                .OrderBy(h => h.DateUtc)
                .ThenBy(h => BuildFullName(h.User.Profile))
                .Select(h => new EventVolunteerDto(
                    UserId: h.UserId,
                    FullName: BuildFullName(h.User.Profile),
                    Email: h.User.Email,
                    ParticipationDateUtc: h.DateUtc,
                    DurationMinutes: h.DurationMinutes
                ))
                .ToList();

            var dto = new EventAssignmentReportDto(
                EventId: ev.Id,
                EventName: ev.Name,
                EventDateUtc: ev.DateUtc,
                Location: ev.Location,
                Urgency: ev.Urgency.ToString(),
                RequiredSkills: ev.RequiredSkills
                    .Select(s => s.ToString())
                    .OrderBy(s => s)
                    .ToList(),
                Volunteers: volunteers
            );

            reports.Add(dto);
        }

        return await fileReportService.GenerateEventAssignmentsReportAsync(
            reports,
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
            return first;

        return $"{first} {last}";
    }
}
