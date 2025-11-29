using System.Linq.Expressions;
using Moq;
using SixSeven.Application.Features.Notifications;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Tests.Features.Notifications
{
    [TestFixture]
    public class MarkAllNotificationsReadHandlerTests
    {
        private Mock<IGenericRepository<Notification>> _notificationsRepo = null!;
        private MarkAllNotificationsReadHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _notificationsRepo = new Mock<IGenericRepository<Notification>>(MockBehavior.Strict);
            _handler = new MarkAllNotificationsReadHandler(_notificationsRepo.Object);
        }

        private static Notification CreateNotification(string userId, string message, bool read)
        {
            var n = new Notification(userId, message);
            typeof(Notification).GetProperty(nameof(Notification.Read))!
                .SetValue(n, read);
            return n;
        }

        [Test]
        public async Task Handle_NoUnreadNotifications_ReturnsZeroAndDoesNotSave()
        {
            var userId = "user-1";

            var n1 = CreateNotification(userId, "Msg 1", true);
            var n2 = CreateNotification(userId, "Msg 2", true);
            var others = new List<Notification> { n1, n2 }.AsQueryable();

            _notificationsRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Notification, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = others.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Notification>>(filtered);
                });

            var result = await _handler.Handle(
                new MarkAllNotificationsReadCommand(userId),
                CancellationToken.None);

            Assert.That(result, Is.EqualTo(0));

            _notificationsRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            _notificationsRepo.Verify(
                r => r.GetAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_UnreadNotifications_MarksAllRead_SavesAndReturnsCount()
        {
            var userId = "user-1";

            var unread1 = CreateNotification(userId, "Unread 1", false);
            var unread2 = CreateNotification(userId, "Unread 2", false);
            var readForUser = CreateNotification(userId, "Already read", true);
            var otherUser = CreateNotification("other-user", "Other", false);

            var all = new List<Notification> { unread1, unread2, readForUser, otherUser }.AsQueryable();

            _notificationsRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Notification, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = all.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Notification>>(filtered);
                });

            _notificationsRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            var result = await _handler.Handle(
                new MarkAllNotificationsReadCommand(userId),
                CancellationToken.None);

            Assert.That(result, Is.EqualTo(2));
            Assert.That(unread1.Read, Is.True);
            Assert.That(unread2.Read, Is.True);
            Assert.That(readForUser.Read, Is.True);
            Assert.That(otherUser.Read, Is.False);

            _notificationsRepo.Verify(
                r => r.GetAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _notificationsRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
