using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Features.Events;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;
using VolunteerHistoryEntity = SixSeven.Domain.Entities.VolunteerHistory;

namespace SixSeven.Application.Tests.Features.Events
{
    [TestFixture]
    public class AssignVolunteerToEventHandlerTests
    {
        private Mock<IGenericRepository<Event>> _eventsRepo = null!;
        private Mock<IGenericRepository<UserCredentials>> _usersRepo = null!;
        private Mock<IGenericRepository<VolunteerHistoryEntity>> _historyRepo = null!;
        private Mock<IGenericRepository<Notification>> _notificationsRepo = null!;
        private AssignVolunteerToEventHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _eventsRepo = new Mock<IGenericRepository<Event>>(MockBehavior.Strict);
            _usersRepo = new Mock<IGenericRepository<UserCredentials>>(MockBehavior.Strict);
            _historyRepo = new Mock<IGenericRepository<VolunteerHistoryEntity>>(MockBehavior.Strict);
            _notificationsRepo = new Mock<IGenericRepository<Notification>>(MockBehavior.Strict);

            _handler = new AssignVolunteerToEventHandler(
                _eventsRepo.Object,
                _usersRepo.Object,
                _historyRepo.Object,
                _notificationsRepo.Object);
        }

        private static Event CreateEvent(string id, DateTime dateUtc)
        {
            var e = new Event(
                "Test Event",
                "Description",
                "Location",
                dateUtc,
                EventUrgency.Medium,
                Array.Empty<VolunteerSkill>());

            typeof(Event).GetProperty(nameof(Event.Id))!
                .SetValue(e, id);

            return e;
        }

        private static UserCredentials CreateVolunteer(string id)
        {
            var u = new UserCredentials("vol@test.com", "hash", UserRole.Volunteer);
            typeof(UserCredentials).GetProperty(nameof(UserCredentials.Id))!
                .SetValue(u, id);
            return u;
        }

        private static VolunteerHistoryEntity CreateHistory(string id, string eventId, string userId, DateTime dateUtc, int minutes)
        {
            var h = new VolunteerHistoryEntity(userId, eventId, dateUtc, minutes);
            typeof(VolunteerHistoryEntity).GetProperty(nameof(VolunteerHistoryEntity.Id))!
                .SetValue(h, id);
            return h;
        }

        [Test]
        public async Task Handle_EventNotFound_ReturnsNullAndDoesNotQueryUserOrHistory()
        {
            _eventsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Event?)null);

            var cmd = new AssignVolunteerToEventCommand("event-1", "vol-1");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.That(result, Is.Null);

            _usersRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _historyRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<VolunteerHistoryEntity, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _eventsRepo.VerifyAll();
        }

        [Test]
        public async Task Handle_VolunteerNotFoundOrWrongRole_ReturnsNull()
        {
            var ev = CreateEvent("event-1", DateTime.UtcNow);

            _eventsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ev);

            _usersRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredentials?)null);

            var cmd = new AssignVolunteerToEventCommand(ev.Id, "vol-1");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.That(result, Is.Null);

            _historyRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<VolunteerHistoryEntity, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _eventsRepo.VerifyAll();
            _usersRepo.VerifyAll();
        }

        [Test]
        public async Task Handle_ExistingHistory_ReturnsExistingAssignmentWithoutCreatingNew()
        {
            var ev = CreateEvent("event-1", DateTime.UtcNow);
            var volunteer = CreateVolunteer("vol-1");
            var existing = CreateHistory("hist-1", ev.Id, volunteer.Id, ev.DateUtc, 45);

            _eventsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ev);

            _usersRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(volunteer);

            _historyRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<VolunteerHistoryEntity, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var cmd = new AssignVolunteerToEventCommand(ev.Id, volunteer.Id, 60);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result!.VolunteerHistoryId, Is.EqualTo(existing.Id));
                Assert.That(result.EventId, Is.EqualTo(existing.EventId));
                Assert.That(result.VolunteerId, Is.EqualTo(existing.UserId));
                Assert.That(result.DateUtc, Is.EqualTo(existing.DateUtc));
                Assert.That(result.DurationMinutes, Is.EqualTo(existing.DurationMinutes));
            });

            _historyRepo.Verify(
                r => r.QueueInsert(It.IsAny<VolunteerHistoryEntity>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _notificationsRepo.Verify(
                r => r.QueueInsert(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _historyRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            _eventsRepo.VerifyAll();
            _usersRepo.VerifyAll();
            _historyRepo.VerifyAll();
        }

        [Test]
        public async Task Handle_NewAssignment_CreatesHistoryNotificationAndSaves()
        {
            var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var ev = CreateEvent("event-1", date);
            var volunteer = CreateVolunteer("vol-1");

            _eventsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ev);

            _usersRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(volunteer);

            _historyRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<VolunteerHistoryEntity, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((VolunteerHistoryEntity?)null);

            VolunteerHistoryEntity? createdHistory = null;
            _historyRepo
                .Setup(r => r.QueueInsert(It.IsAny<VolunteerHistoryEntity>(), It.IsAny<CancellationToken>()))
                .Callback<VolunteerHistoryEntity, CancellationToken>((h, _) => createdHistory = h);

            Notification? createdNotification = null;
            _notificationsRepo
                .Setup(r => r.QueueInsert(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .Callback<Notification, CancellationToken>((n, _) => createdNotification = n);

            _historyRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var cmd = new AssignVolunteerToEventCommand(ev.Id, volunteer.Id, 90);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.That(createdHistory, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(createdHistory!.UserId, Is.EqualTo(volunteer.Id));
                Assert.That(createdHistory.EventId, Is.EqualTo(ev.Id));
                Assert.That(createdHistory.DateUtc, Is.EqualTo(ev.DateUtc));
                Assert.That(createdHistory.DurationMinutes, Is.EqualTo(90));
            });

            Assert.That(createdNotification, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(createdNotification!.UserId, Is.EqualTo(volunteer.Id));
                Assert.That(createdNotification.Message, Does.Contain(ev.Name));
            });

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result!.VolunteerHistoryId, Is.EqualTo(createdHistory!.Id));
                Assert.That(result.EventId, Is.EqualTo(createdHistory.EventId));
                Assert.That(result.VolunteerId, Is.EqualTo(createdHistory.UserId));
                Assert.That(result.DateUtc, Is.EqualTo(createdHistory.DateUtc));
                Assert.That(result.DurationMinutes, Is.EqualTo(createdHistory.DurationMinutes));
            });

            _historyRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _eventsRepo.VerifyAll();
            _usersRepo.VerifyAll();
            _historyRepo.VerifyAll();
            _notificationsRepo.VerifyAll();
        }
    }
}
