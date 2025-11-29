using SixSeven.Domain.Enums;

namespace SixSeven.Domain.DTO;

public sealed class VolunteerHistoryDto
{
    public string Id { get; init; } = null!; // Event Id (for the frontend)
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Location { get; init; } = null!;

    public string DateIsoString { get; init; } = null!;

    public EventUrgency Urgency { get; init; }

    public IReadOnlyList<VolunteerSkill> RequiredSkills { get; init; } = [];

    public string TimeAtEvent { get; init; } = null!;
}