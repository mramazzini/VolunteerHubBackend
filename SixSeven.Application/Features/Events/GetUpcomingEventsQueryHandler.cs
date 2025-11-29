using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Mappers;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Events;

public record GetUpcomingEventsQuery 
    : IRequest<IReadOnlyList<EventDto>>;

public sealed class GetUpcomingEventsQueryHandler(IGenericRepository<Event> eventRepository)
    : IRequestHandler<GetUpcomingEventsQuery, IReadOnlyList<EventDto>>
{
    public async Task<IReadOnlyList<EventDto>> Handle(
        GetUpcomingEventsQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var events = await eventRepository.GetAsync(
            e => e.DateUtc > nowUtc,
            cancellationToken);

        return events
            .OrderBy(e => e.DateUtc)
            .ToDtos();
    }
}