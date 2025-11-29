namespace SixSeven.Domain.Models;

public sealed record AssignVolunteerToEventResult(
    string VolunteerHistoryId,
    string EventId,
    string VolunteerId,
    DateTime DateUtc,
    int DurationMinutes);
