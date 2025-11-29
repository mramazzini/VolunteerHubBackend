using System;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Mappers;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Tests.Mappers;

[TestFixture]
public class NotificationDtoMapperTests
{
    private static Notification CreateNotification(
        string id = "notif-1",
        string userId = "user-1",
        string message = "Test message",
        bool read = false,
        DateTime? createdAt = null)
    {
        var n = new Notification(userId, message);

        typeof(Notification).GetProperty(nameof(Notification.Id))!
            .SetValue(n, id);

        typeof(Notification).GetProperty(nameof(Notification.Read))!
            .SetValue(n, read);

        typeof(Notification).GetProperty(nameof(Notification.CreatedAt))!
            .SetValue(n, createdAt ?? DateTime.UtcNow);

        return n;
    }

    [Test]
    public void ToDto_MapsAllFieldsCorrectly()
    {
        var created = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var notification = CreateNotification(
            id: "n-123",
            userId: "user-9",
            message: "Hello",
            read: true,
            createdAt: created);

        var dto = notification.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(notification.Id));
            Assert.That(dto.UserId, Is.EqualTo(notification.UserId));
            Assert.That(dto.Message, Is.EqualTo(notification.Message));
            Assert.That(dto.Read, Is.EqualTo(notification.Read));
            Assert.That(dto.CreatedAt, Is.EqualTo(notification.CreatedAt));
        });
    }

    [Test]
    public void ToDto_UnreadNotification_ReadIsFalse()
    {
        var notification = CreateNotification(read: false);

        var dto = notification.ToDto();

        Assert.That(dto.Read, Is.False);
    }
}