namespace SixSeven.Domain.DTO;

public sealed record EventVolunteerDto(
    string UserId,
    string FullName,
    string Email,
    DateTime ParticipationDateUtc,
    int DurationMinutes);

public sealed record EventAssignmentReportDto(
    string EventId,
    string EventName,
    DateTime EventDateUtc,
    string Location,
    string Urgency,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<EventVolunteerDto> Volunteers);