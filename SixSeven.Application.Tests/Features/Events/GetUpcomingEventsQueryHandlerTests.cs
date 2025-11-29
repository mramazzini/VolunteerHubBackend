using System.Linq.Expressions;
using Moq;
using SixSeven.Application.Features.Events;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Events
{
    [TestFixture]
    public class GetUpcomingEventsQueryHandlerTests
    {
        private Mock<IGenericRepository<Event>> _eventRepo = null!;
        private GetUpcomingEventsQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _eventRepo = new Mock<IGenericRepository<Event>>(MockBehavior.Strict);
            _handler = new GetUpcomingEventsQueryHandler(_eventRepo.Object);
        }

        private static Event CreateEvent(string name, DateTime dateUtc)
        {
            return new Event(
                name,
                "Description",
                "Location",
                dateUtc,
                EventUrgency.Medium,
                Array.Empty<VolunteerSkill>());
        }

        [Test]
        public async Task Handle_ReturnsOnlyFutureEvents_OrderedByDate()
        {
            var now = DateTime.UtcNow;

            var pastEvent = CreateEvent("Past Event", now.AddMinutes(-30));
            var futureEvent1 = CreateEvent("Future Event 1", now.AddMinutes(10));
            var futureEvent2 = CreateEvent("Future Event 2", now.AddMinutes(20));

            var allEvents = new List<Event>
            {
                pastEvent,
                futureEvent2,
                futureEvent1
            }.AsQueryable();

            _eventRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Event, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = allEvents.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Event>>(filtered);
                });

            var result = await _handler.Handle(
                new GetUpcomingEventsQuery(),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));

            var names = result.Select(e => e.Name).ToList();
            Assert.That(names, Is.EqualTo(new[] { "Future Event 1", "Future Event 2" }));

            _eventRepo.Verify(r =>
                r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_NoUpcomingEvents_ReturnsEmptyList()
        {
            var now = DateTime.UtcNow;

            var pastEvent1 = CreateEvent("Past 1", now.AddMinutes(-60));
            var pastEvent2 = CreateEvent("Past 2", now.AddMinutes(-10));

            var allEvents = new List<Event>
            {
                pastEvent1,
                pastEvent2
            }.AsQueryable();

            _eventRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Event, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = allEvents.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Event>>(filtered);
                });

            var result = await _handler.Handle(
                new GetUpcomingEventsQuery(),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);

            _eventRepo.Verify(r =>
                r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
