using NUnit.Framework;
using SixSeven.Domain.Entities;

namespace SixSeven.Domain.Tests
{
    [TestFixture]
    public class VolunteerHistoryTests
    {
        [Test]
        public void Constructor_ValidArguments_SetsPropertiesCorrectly()
        {
            var userId = "user-123";
            var eventId = "event-456";
            var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);
            var durationMinutes = 120;

            var history = new VolunteerHistory(userId, eventId, date, durationMinutes);

            Assert.Multiple(() =>
            {
                Assert.That(history.Id, Is.Not.Null.And.Not.Empty);
                Assert.That(history.UserId, Is.EqualTo(userId));
                Assert.That(history.EventId, Is.EqualTo(eventId));
                Assert.That(history.DurationMinutes, Is.EqualTo(durationMinutes));
                Assert.That(history.DateUtc, Is.EqualTo(date));
                Assert.That(history.DateUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(history.CreatedAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(history.CreatedAtUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
            });
        }

        [Test]
        public void Constructor_SetsDateKindToUtc_WhenLocalOrUnspecifiedProvided()
        {
            var localDate = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Local);

            var history = new VolunteerHistory("user-123", "event-456", localDate, 60);

            Assert.Multiple(() =>
            {
                Assert.That(history.DateUtc, Is.EqualTo(localDate));
                Assert.That(history.DateUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidUserId_ThrowsArgumentException(string? invalidUserId)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new VolunteerHistory(invalidUserId!, "event-123", DateTime.UtcNow, 60));

            Assert.That(ex!.ParamName, Is.EqualTo("userId"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidEventId_ThrowsArgumentException(string? invalidEventId)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new VolunteerHistory("user-123", invalidEventId!, DateTime.UtcNow, 60));

            Assert.That(ex!.ParamName, Is.EqualTo("eventId"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-30)]
        public void Constructor_NonPositiveDuration_ThrowsArgumentOutOfRangeException(int invalidDuration)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new VolunteerHistory("user-123", "event-456", DateTime.UtcNow, invalidDuration));

            Assert.That(ex!.ParamName, Is.EqualTo("durationMinutes"));
        }

        [Test]
        public void UpdateDuration_ValidValue_UpdatesDuration()
        {
            var history = new VolunteerHistory("user-123", "event-456", DateTime.UtcNow, 60);

            history.UpdateDuration(90);

            Assert.That(history.DurationMinutes, Is.EqualTo(90));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-15)]
        public void UpdateDuration_NonPositive_ThrowsArgumentOutOfRangeException(int invalidDuration)
        {
            var history = new VolunteerHistory("user-123", "event-456", DateTime.UtcNow, 60);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                history.UpdateDuration(invalidDuration));

            Assert.That(ex!.ParamName, Is.EqualTo("durationMinutes"));
        }

        [Test]
        public void Reschedule_UpdatesDateAndSetsKindToUtc()
        {
            var originalDate = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var newDate = new DateTime(2025, 2, 1, 9, 30, 0, DateTimeKind.Unspecified);

            var history = new VolunteerHistory("user-123", "event-456", originalDate, 60);

            history.Reschedule(newDate);

            Assert.Multiple(() =>
            {
                Assert.That(history.DateUtc, Is.EqualTo(newDate));
                Assert.That(history.DateUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }
    }
}
