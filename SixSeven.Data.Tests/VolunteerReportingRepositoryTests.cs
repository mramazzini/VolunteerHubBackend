using Microsoft.EntityFrameworkCore;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Data.Tests
{
    [TestFixture]
    public class VolunteerReportingRepositoryTests
    {
        private AppDbContext _context = null!;
        private VolunteerReportingRepository _repository = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"sixseven-reporting-{Guid.NewGuid():N}")
                .Options;

            _context = new AppDbContext(options);
            _repository = new VolunteerReportingRepository(_context);
        }

        private UserCredentials CreateUser(string email, string firstName, string lastName)
        {
            var user = new UserCredentials(email, "hash");
            var profile = new UserProfile(
                user.Id,
                firstName,
                lastName,
                "123 Main St",
                "Houston",
                "TX",
                "77001",
                "Prefs",
                new[] { VolunteerSkill.Cooking },
                null,
                new[] { "Saturday" });

            user.AttachProfile(profile);

            _context.UserCredentials.Add(user);
            _context.UserProfiles.Add(profile);

            return user;
        }

        private Event CreateEvent(string name, DateTime dateUtc)
        {
            var ev = new Event(
                name,
                "Description",
                "Location",
                dateUtc,
                EventUrgency.Medium,
                new[] { VolunteerSkill.Cooking });

            _context.Events.Add(ev);
            return ev;
        }

        private VolunteerHistory CreateHistory(UserCredentials user, Event ev, DateTime dateUtc, int minutes)
        {
            var history = new VolunteerHistory(user.Id, ev.Id, dateUtc, minutes);
            _context.VolunteerHistories.Add(history);
            return history;
        }

        [Test]
        public async Task GetVolunteerActivityAsync_NoFilters_ReturnsAllOrderedAndIncludesNavigation()
        {
            var user1 = CreateUser("u1@test.com", "Alice", "A");
            var user2 = CreateUser("u2@test.com", "Bob", "B");

            var date1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var date2 = new DateTime(2025, 1, 2, 10, 0, 0, DateTimeKind.Utc);

            var ev1 = CreateEvent("Event X", date2);
            var ev2 = CreateEvent("Event Y", date1);

            var h1 = CreateHistory(user1, ev1, date2, 60);
            var h2 = CreateHistory(user2, ev2, date1, 30);
            var h3 = CreateHistory(user1, ev2, date1, 45);

            await _context.SaveChangesAsync();

            var results = await _repository.GetVolunteerActivityAsync(null, null, CancellationToken.None);

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.Select(h => h.Id), Is.EquivalentTo(new[] { h1.Id, h2.Id, h3.Id }));

            var ordered = results.ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                Assert.That(ordered[i - 1].DateUtc, Is.LessThanOrEqualTo(ordered[i].DateUtc));
            }

            Assert.That(ordered[0].User, Is.Not.Null);
            Assert.That(ordered[0].User.Profile, Is.Not.Null);
            Assert.That(ordered[0].Event, Is.Not.Null);
        }

        [Test]
        public async Task GetVolunteerActivityAsync_FromFilter_FiltersLowerBoundInclusive()
        {
            var user = CreateUser("u@test.com", "Test", "User");

            var d1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            var e1 = CreateEvent("E1", d1);
            var e2 = CreateEvent("E2", d2);
            var e3 = CreateEvent("E3", d3);

            CreateHistory(user, e1, d1, 30);
            var h2 = CreateHistory(user, e2, d2, 45);
            var h3 = CreateHistory(user, e3, d3, 60);

            await _context.SaveChangesAsync();

            var from = d2;
            var results = await _repository.GetVolunteerActivityAsync(from, null, CancellationToken.None);

            Assert.That(results.Select(h => h.Id), Is.EquivalentTo(new[] { h2.Id, h3.Id }));
        }

        [Test]
        public async Task GetVolunteerActivityAsync_ToFilter_FiltersUpperBoundInclusive()
        {
            var user = CreateUser("u@test.com", "Test", "User");

            var d1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            var e1 = CreateEvent("E1", d1);
            var e2 = CreateEvent("E2", d2);
            var e3 = CreateEvent("E3", d3);

            var h1 = CreateHistory(user, e1, d1, 30);
            var h2 = CreateHistory(user, e2, d2, 45);
            CreateHistory(user, e3, d3, 60);

            await _context.SaveChangesAsync();

            var to = d2;
            var results = await _repository.GetVolunteerActivityAsync(null, to, CancellationToken.None);

            Assert.That(results.Select(h => h.Id), Is.EquivalentTo(new[] { h1.Id, h2.Id }));
        }

        [Test]
        public async Task GetVolunteerActivityAsync_FromAndToFilters_ApplyRange()
        {
            var user = CreateUser("u@test.com", "Test", "User");

            var d1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            var e1 = CreateEvent("E1", d1);
            var e2 = CreateEvent("E2", d2);
            var e3 = CreateEvent("E3", d3);

            CreateHistory(user, e1, d1, 30);
            var h2 = CreateHistory(user, e2, d2, 45);
            CreateHistory(user, e3, d3, 60);

            await _context.SaveChangesAsync();

            var from = d2;
            var to = d2;
            var results = await _repository.GetVolunteerActivityAsync(from, to, CancellationToken.None);

            Assert.That(results.Select(h => h.Id), Is.EquivalentTo(new[] { h2.Id }));
        }

        [Test]
        public async Task GetEventsScheduledAsync_NoFilters_ReturnsAllOrdered()
        {
            var d1 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var e1 = CreateEvent("Z Event", d1);
            var e2 = CreateEvent("A Event", d2);
            var e3 = CreateEvent("M Event", d2);
            var e4 = CreateEvent("B Event", d3);

            await _context.SaveChangesAsync();

            var results = await _repository.GetEventsScheduledAsync(null, null, CancellationToken.None);

            var ordered = results.ToList();
            Assert.That(ordered.Select(e => e.Id), Is.EqualTo(new[]
            {
                e2.Id, e3.Id, e4.Id, e1.Id
            }));
        }

        [Test]
        public async Task GetEventsScheduledAsync_FromFilter_FiltersLowerBoundInclusive()
        {
            var d1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            var e1 = CreateEvent("E1", d1);
            var e2 = CreateEvent("E2", d2);
            var e3 = CreateEvent("E3", d3);

            await _context.SaveChangesAsync();

            var from = d2;
            var results = await _repository.GetEventsScheduledAsync(from, null, CancellationToken.None);

            Assert.That(results.Select(e => e.Id), Is.EquivalentTo(new[] { e2.Id, e3.Id }));
        }

        [Test]
        public async Task GetEventsScheduledAsync_ToFilter_FiltersUpperBoundInclusive()
        {
            var d1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            var e1 = CreateEvent("E1", d1);
            var e2 = CreateEvent("E2", d2);
            CreateEvent("E3", d3);

            await _context.SaveChangesAsync();

            var to = d2;
            var results = await _repository.GetEventsScheduledAsync(null, to, CancellationToken.None);

            Assert.That(results.Select(e => e.Id), Is.EquivalentTo(new[] { e1.Id, e2.Id }));
        }

        [Test]
        public async Task GetEventsScheduledAsync_FromAndToFilters_ApplyRange()
        {
            var d1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var d2 = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc);
            var d3 = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc);

            CreateEvent("E1", d1);
            var e2 = CreateEvent("E2", d2);
            CreateEvent("E3", d3);

            await _context.SaveChangesAsync();

            var from = d2;
            var to = d2;
            var results = await _repository.GetEventsScheduledAsync(from, to, CancellationToken.None);

            Assert.That(results.Select(e => e.Id), Is.EquivalentTo(new[] { e2.Id }));
        }
    }
}
