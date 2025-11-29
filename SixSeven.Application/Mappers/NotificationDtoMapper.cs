using SixSeven.Application.Dtos;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Mappers;

public static class NotificationDtoMapper
{
    public static NotificationDto ToDto(this Notification notification) =>
        new()
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Message = notification.Message,
            Read = notification.Read,
            CreatedAt = notification.CreatedAt
        };
}
