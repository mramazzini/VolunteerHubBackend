namespace SixSeven.Domain.Models;

public sealed class AssignVolunteerRequest
{
    public string VolunteerId { get; init; } = string.Empty;
    public int DurationMinutes { get; init; } = 60;
}
