using SixSeven.Domain.Enums;

namespace SixSeven.Domain.DTO;

public sealed record VolunteerActivityRowDto(
    string UserId,
    string FullName,
    string Email,
    string EventId,
    string EventName,
    DateTime EventDateUtc,
    int DurationMinutes);

