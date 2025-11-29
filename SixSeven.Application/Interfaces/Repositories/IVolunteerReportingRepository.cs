using SixSeven.Domain.Entities;

namespace SixSeven.Application.Interfaces.Repositories;

public interface IVolunteerReportingRepository
{
    Task<IReadOnlyList<VolunteerHistory>> GetVolunteerActivityAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetEventsScheduledAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
}