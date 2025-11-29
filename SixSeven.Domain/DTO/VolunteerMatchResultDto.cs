using SixSeven.Domain.Enums;

namespace SixSeven.Domain.DTO;

public sealed record VolunteerMatchResultDto(
    string VolunteerId,
    string VolunteerName,
    EventDto BestEvent,
    int Score,
    IReadOnlyList<VolunteerSkill> OverlappingSkills,
    IReadOnlyList<VolunteerSkill> MissingSkills,
    IReadOnlyList<VolunteerSkill> ExtraSkills);