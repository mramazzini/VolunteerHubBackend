using Microsoft.EntityFrameworkCore;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Data.Tests
{
    [TestFixture]
    public class GenericRepositoryTests
    {
        private AppDbContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"sixseven-tests-{Guid.NewGuid():N}")
                .Options;

            _context = new AppDbContext(options);
        }

        private static Event CreateEvent(string idSuffix = "")
        {
            return new Event(
                $"Event{idSuffix}",
                "Description",
                "Location",
                new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                EventUrgency.Low,
                Array.Empty<VolunteerSkill>());
        }

        [Test]
        public async Task QueueInsert_ThenSaveAsync_PersistsEntity()
        {
            var repo = new GenericRepository<Event>(_context);
            var ev = CreateEvent();

            repo.QueueInsert(ev);
            var changed = await repo.SaveAsync();

            Assert.That(changed, Is.GreaterThanOrEqualTo(1));

            var fromDb = await _context.Events.SingleOrDefaultAsync();
            Assert.That(fromDb, Is.Not.Null);
            Assert.That(fromDb!.Name, Is.EqualTo(ev.Name));
        }

        [Test]
        public void QueueInsert_NullEntity_Throws()
        {
            var repo = new GenericRepository<Event>(_context);

            Assert.Throws<ArgumentNullException>(() => repo.QueueInsert(null!));
        }

        [Test]
        public async Task FindAsync_MatchingPredicate_ReturnsEntity()
        {
            var repo = new GenericRepository<Event>(_context);
            var ev1 = CreateEvent("1");
            var ev2 = CreateEvent("2");

            _context.Events.AddRange(ev1, ev2);
            await _context.SaveChangesAsync();

            var result = await repo.FindAsync(e => e.Name == ev2.Name);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo(ev2.Name));
        }

        [Test]
        public async Task FindAsync_NoMatch_ReturnsNull()
        {
            var repo = new GenericRepository<Event>(_context);
            _context.Events.Add(CreateEvent("1"));
            await _context.SaveChangesAsync();

            var result = await repo.FindAsync(e => e.Name == "DoesNotExist");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindAsync_NullPredicate_Throws()
        {
            var repo = new GenericRepository<Event>(_context);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await repo.FindAsync(null!));
        }

        [Test]
        public async Task GetAsync_WithMatches_ReturnsAllMatching()
        {
            var repo = new GenericRepository<Event>(_context);

            var ev1 = new Event("A", "Desc", "Loc", DateTime.UtcNow, EventUrgency.Low, Array.Empty<VolunteerSkill>());
            var ev2 = new Event("B", "Desc", "Loc", DateTime.UtcNow, EventUrgency.Low, Array.Empty<VolunteerSkill>());
            var ev3 = new Event("A", "Desc", "Loc", DateTime.UtcNow, EventUrgency.Low, Array.Empty<VolunteerSkill>());

            _context.Events.AddRange(ev1, ev2, ev3);
            await _context.SaveChangesAsync();

            IReadOnlyList<Event> results = await repo.GetAsync(e => e.Name == "A");

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Select(e => e.Name).Distinct().Single(), Is.EqualTo("A"));
        }

        [Test]
        public async Task GetAsync_NoMatches_ReturnsEmptyList()
        {
            var repo = new GenericRepository<Event>(_context);

            _context.Events.Add(CreateEvent("1"));
            await _context.SaveChangesAsync();

            IReadOnlyList<Event> results = await repo.GetAsync(e => e.Name == "DoesNotExist");

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void GetAsync_NullPredicate_Throws()
        {
            var repo = new GenericRepository<Event>(_context);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await repo.GetAsync(null!));
        }
    }
}
