using SixSeven.Domain.Entities;

namespace SixSeven.Domain.Entities;

public class VolunteerHistory
{
    // EF
    protected VolunteerHistory() { }

    public VolunteerHistory(
        string userId,
        string eventId,
        DateTime dateUtc,
        int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("EventId is required.", nameof(eventId));

        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be positive.");

        Id = Guid.NewGuid().ToString("N");
        UserId = userId;
        EventId = eventId;
        DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);
        DurationMinutes = durationMinutes;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Id { get; private set; } = null!;

    public string UserId { get; private set; } = null!;
    public string EventId { get; private set; } = null!;

    /// <summary>
    /// The date/time the volunteer participated in the event (UTC).
    /// </summary>
    public DateTime DateUtc { get; private set; }

    /// <summary>
    /// Duration the volunteer spent at the event, in minutes.
    /// </summary>
    public int DurationMinutes { get; private set; }

    /// <summary>
    /// When this history record was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    public UserCredentials User { get; private set; } = null!;
    public Event Event { get; private set; } = null!;

    public void UpdateDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be positive.");

        DurationMinutes = durationMinutes;
    }

    public void Reschedule(DateTime newDateUtc)
    {
        DateUtc = DateTime.SpecifyKind(newDateUtc, DateTimeKind.Utc);
    }
}
