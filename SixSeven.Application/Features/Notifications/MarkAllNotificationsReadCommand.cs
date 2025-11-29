using MediatR;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Notifications;


public sealed record MarkAllNotificationsReadCommand(
    string UserId
) : IRequest<int>;

public sealed class MarkAllNotificationsReadHandler(IGenericRepository<Notification> notifications)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var unread = await notifications.GetAsync(
            n => n.UserId == request.UserId && !n.Read,
            cancellationToken);

        if (unread.Count == 0)
            return 0;

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
        }

        await notifications.SaveAsync(cancellationToken);

        return unread.Count;
    }
}