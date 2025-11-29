namespace SixSeven.Domain.Entities;

public class Notification
{
    protected Notification() { }

    public Notification(string userId, string message)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message is required.", nameof(message));

        Id = Guid.NewGuid().ToString("N");
        UserId = userId;
        Message = message;
        CreatedAt = DateTime.UtcNow;
        Read = false;
    }

    public string Id { get; private set; } = null!;

    public string Message { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public bool Read { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public UserCredentials UserCredentials { get; private set; } = null!;

    public void MarkAsRead()
    {
        Read = true;
    }
}