using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Events;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Events
{
    [TestFixture]
    public class UpsertEventCommandHandlerTests
    {
        private Mock<IGenericRepository<Event>> _eventRepo = null!;
        private UpsertEventCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _eventRepo = new Mock<IGenericRepository<Event>>(MockBehavior.Strict);
            _handler = new UpsertEventCommandHandler(_eventRepo.Object);
        }

        private static Event CreateEvent(
            string name,
            string description,
            string location,
            DateTime dateUtc,
            EventUrgency urgency)
        {
            return new Event(
                name,
                description,
                location,
                dateUtc,
                urgency,
                Array.Empty<VolunteerSkill>());
        }

        [Test]
        public async Task Handle_CreateNewEvent_InsertsAndReturnsEventDto()
        {
            var date = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);

            var command = new UpsertEventCommand(
                Id: null,
                Name: "Food Drive",
                Description: "Help pack food boxes",
                Location: "Community Center",
                DateUtc: date,
                Urgency: "High",
                RequiredSkills: new[] { "Cooking", " DRIVING " });

            Event? inserted = null;

            _eventRepo
                .Setup(r => r.QueueInsert(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
                .Callback<Event, CancellationToken>((e, _) => inserted = e);

            _eventRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(inserted, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(inserted!.Name, Is.EqualTo(command.Name));
                Assert.That(inserted.Description, Is.EqualTo(command.Description));
                Assert.That(inserted.Location, Is.EqualTo(command.Location));
                Assert.That(inserted.DateUtc, Is.EqualTo(command.DateUtc));
                Assert.That(inserted.Urgency, Is.EqualTo(EventUrgency.High));
                Assert.That(inserted.RequiredSkills, Is.EquivalentTo(
                    new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(inserted.Id));
                Assert.That(result.Name, Is.EqualTo(command.Name));
                Assert.That(result.Description, Is.EqualTo(command.Description));
                Assert.That(result.Location, Is.EqualTo(command.Location));
                Assert.That(result.DateIsoString, Is.EqualTo(date.ToString("O")));
                Assert.That(result.Urgency, Is.EqualTo(EventUrgency.High));
                Assert.That(result.RequiredSkills, Is.EquivalentTo(
                    new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving }));
            });

            _eventRepo.Verify(
                r => r.QueueInsert(It.IsAny<Event>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _eventRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _eventRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_UpdateExistingEvent_UpdatesAndReturnsEventDto()
        {
            var originalDate = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var updatedDate = new DateTime(2025, 1, 2, 15, 30, 0, DateTimeKind.Utc);

            var existing = CreateEvent(
                "Old Name",
                "Old Desc",
                "Old Location",
                originalDate,
                EventUrgency.Low);

            typeof(Event)
                .GetProperty(nameof(Event.Id))!
                .SetValue(existing, "event-123");

            var command = new UpsertEventCommand(
                Id: "event-123",
                Name: "New Name",
                Description: "New Desc",
                Location: "New Location",
                DateUtc: updatedDate,
                Urgency: "Medium",
                RequiredSkills: new[] { "Teaching", "Cooking" });

            _eventRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _eventRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(existing.Name, Is.EqualTo(command.Name));
                Assert.That(existing.Description, Is.EqualTo(command.Description));
                Assert.That(existing.Location, Is.EqualTo(command.Location));
                Assert.That(existing.DateUtc, Is.EqualTo(command.DateUtc));
                Assert.That(existing.Urgency, Is.EqualTo(EventUrgency.Medium));
                Assert.That(existing.RequiredSkills, Is.EquivalentTo(
                    new[] { VolunteerSkill.Teaching, VolunteerSkill.Cooking }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(existing.Id));
                Assert.That(result.DateIsoString, Is.EqualTo(updatedDate.ToString("O")));
                Assert.That(result.Urgency, Is.EqualTo(EventUrgency.Medium));
            });

            _eventRepo.Verify(
                r => r.QueueInsert(It.IsAny<Event>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _eventRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _eventRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void Handle_InvalidUrgency_ThrowsArgumentException()
        {
            var command = new UpsertEventCommand(
                Id: null,
                Name: "Test",
                Description: "Desc",
                Location: "Loc",
                DateUtc: DateTime.UtcNow,
                Urgency: "BadUrgency",
                RequiredSkills: Array.Empty<string>());

            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Invalid urgency value"));
        }

        [Test]
        public void Handle_InvalidSkill_ThrowsArgumentException()
        {
            var command = new UpsertEventCommand(
                Id: null,
                Name: "Test",
                Description: "Desc",
                Location: "Loc",
                DateUtc: DateTime.UtcNow,
                Urgency: "Low",
                RequiredSkills: new[] { "Cooking", "FakeSkill" });

            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Invalid skill value"));
        }

        [Test]
        public void Handle_UpdateMissingEvent_ThrowsInvalidOperationException()
        {
            var command = new UpsertEventCommand(
                Id: "missing-id",
                Name: "Test",
                Description: "Desc",
                Location: "Loc",
                DateUtc: DateTime.UtcNow,
                Urgency: "Low",
                RequiredSkills: Array.Empty<string>());

            _eventRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Event?)null);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Event with id 'missing-id' not found."));
        }
    }
}
