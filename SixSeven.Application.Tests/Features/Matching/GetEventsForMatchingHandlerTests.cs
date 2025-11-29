using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Matching;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Matching
{
    [TestFixture]
    public class GetEventsForMatchingHandlerTests
    {
        private Mock<IGenericRepository<Event>> _eventsRepo = null!;
        private GetEventsForMatchingHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _eventsRepo = new Mock<IGenericRepository<Event>>(MockBehavior.Strict);
            _handler = new GetEventsForMatchingHandler(_eventsRepo.Object);
        }

        private static Event CreateEvent(
            string name,
            DateTime dateUtc,
            EventUrgency urgency = EventUrgency.Medium)
        {
            return new Event(
                name,
                "Description",
                "Location",
                dateUtc,
                urgency,
                Array.Empty<VolunteerSkill>());
        }

        [Test]
        public async Task Handle_ReturnsFutureEvents_OrderedByDate()
        {
            var now = DateTime.UtcNow;
            var past = CreateEvent("Past Event", now.AddMinutes(-30));
            var future1 = CreateEvent("Future Event 1", now.AddMinutes(10));
            var future2 = CreateEvent("Future Event 2", now.AddMinutes(20));
            var future3 = CreateEvent("Future Event 3", now.AddMinutes(30));

            var all = new List<Event> { past, future3, future1, future2 }.AsQueryable();

            _eventsRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Event, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = all.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Event>>(filtered);
                });

            var result = await _handler.Handle(
                new GetEventsForMatchingQuery(),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));

            var names = result.Select(e => e.Name).ToList();
            Assert.That(names, Is.EqualTo(new[]
            {
                "Future Event 1",
                "Future Event 2",
                "Future Event 3"
            }));

            var dates = result.Select(e => DateTime.Parse(e.DateIsoString)).ToList();
            Assert.That(dates, Is.Ordered);

            _eventsRepo.Verify(
                r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Test]
        public async Task Handle_NoCurrentOrFutureEvents_ReturnsEmptyList()
        {
            var now = DateTime.UtcNow;
            var past1 = CreateEvent("Past 1", now.AddMinutes(-60));
            var past2 = CreateEvent("Past 2", now.AddMinutes(-5));

            var all = new List<Event> { past1, past2 }.AsQueryable();

            _eventsRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Event, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Event, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = all.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Event>>(filtered);
                });

            var result = await _handler.Handle(
                new GetEventsForMatchingQuery(),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);

            _eventsRepo.Verify(
                r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
