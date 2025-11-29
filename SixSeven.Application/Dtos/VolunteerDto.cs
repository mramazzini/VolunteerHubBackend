using SixSeven.Domain.Enums;

namespace SixSeven.Application.Dtos;

public sealed record VolunteerDto(
    string Id,
    string Name,
    IReadOnlyList<VolunteerSkill> Skills,
    List<string> Availability);