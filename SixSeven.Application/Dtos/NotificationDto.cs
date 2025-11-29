namespace SixSeven.Application.Dtos;

public sealed class NotificationDto
{
    public string Id { get; init; } = null!;

    public string UserId { get; init; } = null!;
    public string Message { get; init; } = null!;

    public bool Read { get; init; }

    public DateTime CreatedAt { get; init; }
}
