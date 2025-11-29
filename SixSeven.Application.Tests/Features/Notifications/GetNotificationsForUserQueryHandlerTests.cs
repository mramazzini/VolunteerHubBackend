using System.Linq.Expressions;
using Moq;
using SixSeven.Application.Features.Notifications;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Tests.Features.Notifications
{
    [TestFixture]
    public class GetNotificationsForUserQueryHandlerTests
    {
        private Mock<IGenericRepository<Notification>> _notificationRepo = null!;
        private GetNotificationsForUserQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _notificationRepo = new Mock<IGenericRepository<Notification>>(MockBehavior.Strict);
            _handler = new GetNotificationsForUserQueryHandler(_notificationRepo.Object);
        }

        private static Notification CreateNotification(
            string userId,
            string message,
            DateTime createdAtUtc)
        {
            var n = new Notification(userId, message);

            typeof(Notification).GetProperty(nameof(Notification.CreatedAt))!
                .SetValue(n, createdAtUtc);

            return n;
        }

        [Test]
        public async Task Handle_ReturnsOnlyUserNotifications_OrderedByCreatedAtDescending()
        {
            var userId = "user-1";
            var otherUserId = "user-2";

            var n1 = CreateNotification(userId, "First", new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc));
            var n2 = CreateNotification(userId, "Second", new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            var n3 = CreateNotification(userId, "Third", new DateTime(2025, 1, 1, 11, 0, 0, DateTimeKind.Utc));
            var nOther = CreateNotification(otherUserId, "Other", new DateTime(2025, 1, 1, 13, 0, 0, DateTimeKind.Utc));

            var all = new List<Notification> { n1, n2, n3, nOther }.AsQueryable();

            _notificationRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Notification, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = all.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Notification>>(filtered);
                });

            var result = await _handler.Handle(
                new GetNotificationsForUserQuery(userId),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            var messages = result.Select(r => r.Message).ToList();
            Assert.That(messages, Is.EqualTo(new[]
            {
                "Second",
                "Third",
                "First"
            }));

            var createdDates = result
                .Select(r => DateTime.Parse(r.CreatedAt.ToString()))
                .ToList();

            Assert.That(createdDates, Is.Ordered.Descending);

            _notificationRepo.Verify(
                r => r.GetAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_NoNotificationsForUser_ReturnsEmptyList()
        {
            var userId = "user-1";

            var other = CreateNotification("other-user", "Other", new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc));
            var all = new List<Notification> { other }.AsQueryable();

            _notificationRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Notification, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Notification, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = all.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Notification>>(filtered);
                });

            var result = await _handler.Handle(
                new GetNotificationsForUserQuery(userId),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);

            _notificationRepo.Verify(
                r => r.GetAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
