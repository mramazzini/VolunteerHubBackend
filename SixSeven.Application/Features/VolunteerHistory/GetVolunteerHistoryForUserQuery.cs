using MediatR;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Mappers;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.VolunteerHistory;

public record GetVolunteerHistoryForUserQuery(string UserId)
    : IRequest<IReadOnlyList<VolunteerHistoryDto>>;

public sealed class GetVolunteerHistoryForUserQueryHandler(
    IGenericRepository<Domain.Entities.VolunteerHistory> historyRepo,
    IGenericRepository<Event> eventRepo)
    : IRequestHandler<GetVolunteerHistoryForUserQuery, IReadOnlyList<VolunteerHistoryDto>>
{
    public async Task<IReadOnlyList<VolunteerHistoryDto>> Handle(
        GetVolunteerHistoryForUserQuery request,
        CancellationToken cancellationToken)
    {
        var histories = await historyRepo.GetAsync(
            h => h.UserId == request.UserId,
            cancellationToken);

        if (histories.Count == 0)
            return Array.Empty<VolunteerHistoryDto>();

        var eventIds = histories
            .Select(h => h.EventId)
            .Distinct()
            .ToList();

        var events = await eventRepo.GetAsync(
            e => eventIds.Contains(e.Id),
            cancellationToken);

        var eventsById = events.ToDictionary(e => e.Id);

        return histories
            .OrderByDescending(h => h.DateUtc)
            .Where(h => eventsById.ContainsKey(h.EventId))
            .Select(h => h.ToDto(eventsById[h.EventId]))
            .ToList();
    }
}