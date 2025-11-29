using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Mappers;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Matching;

public sealed record GetEventsForMatchingQuery
    : IRequest<IReadOnlyList<EventDto>>;
    
public sealed class GetEventsForMatchingHandler(IGenericRepository<Event> eventsRepo)
    : IRequestHandler<GetEventsForMatchingQuery, IReadOnlyList<EventDto>>
{
    public async Task<IReadOnlyList<EventDto>> Handle(
        GetEventsForMatchingQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var events = await eventsRepo.GetAsync(
            e => e.DateUtc >= nowUtc,
            cancellationToken);

        return events
            .OrderBy(e => e.DateUtc)
            .Select(e =>e.ToDto())
            .ToList();
    }
}
