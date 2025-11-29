using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Mappers;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Notifications;

public record GetNotificationsForUserQuery(string UserId)
    : IRequest<IReadOnlyList<NotificationDto>>;

public sealed class GetNotificationsForUserQueryHandler(IGenericRepository<Notification> notificationRepository)
    : IRequestHandler<GetNotificationsForUserQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetNotificationsForUserQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = await notificationRepository.GetAsync(
            n => n.UserId == request.UserId,
            cancellationToken);

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => n.ToDto())
            .ToList();
    }
}