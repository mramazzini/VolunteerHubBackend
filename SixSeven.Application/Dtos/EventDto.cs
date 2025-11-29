using SixSeven.Domain.Enums;

namespace SixSeven.Application.Dtos;

public sealed class EventDto
{
    public string Id { get; init; } = null!;

    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Location { get; init; } = null!;

    public string DateIsoString { get; init; } = null!;

    public EventUrgency Urgency { get; init; }

    public IReadOnlyList<VolunteerSkill> RequiredSkills { get; init; } = [];
}
