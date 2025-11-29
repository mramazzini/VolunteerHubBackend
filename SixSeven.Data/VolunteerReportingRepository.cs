using Microsoft.EntityFrameworkCore;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.Entities;

namespace SixSeven.Data;

public sealed class VolunteerReportingRepository(AppDbContext dbContext) : IVolunteerReportingRepository
{
    public async Task<IReadOnlyList<VolunteerHistory>> GetVolunteerActivityAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        IQueryable<VolunteerHistory> query = dbContext.VolunteerHistories
            .AsNoTracking()
            .Include(h => h.User)
            .ThenInclude(u => u.Profile)
            .Include(h => h.Event);

        if (fromUtc.HasValue)
        {
            query = query.Where(h => h.DateUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(h => h.DateUtc <= toUtc.Value);
        }

        return await query
            .OrderBy(h => h.DateUtc)
            .ThenBy(h => h.UserId)
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<Event>> GetEventsScheduledAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Events
            .AsNoTracking();

        if (fromUtc.HasValue)
        {
            query = query.Where(e => e.DateUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => e.DateUtc <= toUtc.Value);
        }

        return await query
            .OrderBy(e => e.DateUtc)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }
}