using NUnit.Framework;
using SixSeven.Domain.Entities;

namespace SixSeven.Domain.Tests
{
    [TestFixture]
    public class NotificationTests
    {
        [Test]
        public void Constructor_ValidArguments_SetsPropertiesCorrectly()
        {
            var userId = "user-123";
            var message = "You have a new assignment.";

            var notification = new Notification(userId, message);

            Assert.Multiple(() =>
            {
                Assert.That(notification.Id, Is.Not.Null.And.Not.Empty);
                Assert.That(notification.UserId, Is.EqualTo(userId));
                Assert.That(notification.Message, Is.EqualTo(message));
                Assert.That(notification.Read, Is.False);
                Assert.That(notification.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
                Assert.That(notification.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidUserId_ThrowsArgumentException(string? invalidUserId)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Notification(invalidUserId!, "Valid message"));

            Assert.That(ex!.ParamName, Is.EqualTo("userId"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidMessage_ThrowsArgumentException(string? invalidMessage)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Notification("user-123", invalidMessage!));

            Assert.That(ex!.ParamName, Is.EqualTo("message"));
        }

        [Test]
        public void MarkAsRead_WhenUnread_SetsReadToTrue()
        {
            var notification = new Notification("user-123", "Test message");

            Assert.That(notification.Read, Is.False);

            notification.MarkAsRead();

            Assert.That(notification.Read, Is.True);
        }

        [Test]
        public void MarkAsRead_WhenAlreadyRead_RemainsTrue()
        {
            var notification = new Notification("user-123", "Test message");
            notification.MarkAsRead();

            notification.MarkAsRead();

            Assert.That(notification.Read, Is.True);
        }
    }
}
