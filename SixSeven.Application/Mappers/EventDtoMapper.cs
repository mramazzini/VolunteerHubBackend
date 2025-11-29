using SixSeven.Application.Dtos;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Mappers;

public static class EventDtoMapper
{
    public static EventDto ToDto(this Event entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new EventDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Location = entity.Location,
            DateIsoString = entity.DateUtc.ToString("O"),
            Urgency = entity.Urgency,
            RequiredSkills = entity.RequiredSkills
        };
    }

    public static IReadOnlyList<EventDto> ToDtos(
        this IEnumerable<Event> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(e => e.ToDto())
            .ToList();
    }
}