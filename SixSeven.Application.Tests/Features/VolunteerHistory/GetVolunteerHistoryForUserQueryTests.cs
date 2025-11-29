using System.Linq.Expressions;
using Moq;
using SixSeven.Application.Features.VolunteerHistory;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.VolunteerHistory;

[TestFixture]
public class GetVolunteerHistoryForUserQueryHandlerTests
{
    private Mock<IGenericRepository<Domain.Entities.VolunteerHistory>> _historyRepo = null!;
    private Mock<IGenericRepository<Event>> _eventRepo = null!;
    private GetVolunteerHistoryForUserQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _historyRepo = new(MockBehavior.Strict);
        _eventRepo = new(MockBehavior.Strict);

        _handler = new GetVolunteerHistoryForUserQueryHandler(
            _historyRepo.Object,
            _eventRepo.Object);
    }

    private static Event CreateEvent(
        string id,
        string name,
        DateTime dateUtc,
        EventUrgency urgency,
        IReadOnlyList<VolunteerSkill>? skills = null)
    {
        var ev = new Event(
            name,
            description: "Desc",
            location: "Hall A",
            dateUtc,
            urgency,
            skills ?? []);

        typeof(Event).GetProperty(nameof(Event.Id))!
            .SetValue(ev, id);

        return ev;
    }

    private static Domain.Entities.VolunteerHistory CreateHistory(
        string id,
        string userId,
        string eventId,
        DateTime dateUtc,
        int durationMinutes)
    {
        var h = new Domain.Entities.VolunteerHistory(userId, eventId, dateUtc, durationMinutes);

        typeof(Domain.Entities.VolunteerHistory).GetProperty(nameof(Domain.Entities.VolunteerHistory.Id))!
            .SetValue(h, id);

        return h;
    }

    [Test]
    public async Task Handle_NoHistory_ReturnsEmpty()
    {
        _historyRepo
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Domain.Entities.VolunteerHistory, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Domain.Entities.VolunteerHistory>());

        var result = await _handler.Handle(
            new GetVolunteerHistoryForUserQuery("user-1"),
            CancellationToken.None);

        Assert.That(result, Is.Empty);

        _eventRepo.Verify(
            r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_HistoryExists_ReturnsMappedDtos_OrderedDescending()
    {
        var userId = "user-1";

        var ev1 = CreateEvent(
            id: "event-1",
            name: "Food Drive",
            dateUtc: new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.Low,
            skills: new[] { VolunteerSkill.Cooking });

        var ev2 = CreateEvent(
            id: "event-2",
            name: "Blood Drive",
            dateUtc: new DateTime(2025, 1, 5, 14, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.High,
            skills: new[] { VolunteerSkill.MedicalAid });

        var h1 = CreateHistory(
            id: "h1",
            userId,
            ev1.Id,
            new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            90);

        var h2 = CreateHistory(
            id: "h2",
            userId,
            ev2.Id,
            new DateTime(2025, 1, 5, 15, 0, 0, DateTimeKind.Utc),
            45);

        var histories = new[] { h2, h1 };

        _historyRepo
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Domain.Entities.VolunteerHistory, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(histories);

        _eventRepo
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Expression<Func<Event, bool>>, CancellationToken>((predicate, _) =>
            {
                var func = predicate.Compile();
                var filtered = new[] { ev1, ev2 }
                    .Where(func)
                    .ToList();

                return Task.FromResult<IReadOnlyList<Event>>(filtered);
            });

        var result = await _handler.Handle(
            new GetVolunteerHistoryForUserQuery(userId),
            CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(2));

        var first = result[0];
        var second = result[1];

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo(ev1.Id));
            Assert.That(first.Name, Is.EqualTo(ev1.Name));
            Assert.That(first.Description, Is.EqualTo("Desc"));
            Assert.That(first.Location, Is.EqualTo("Hall A"));
            Assert.That(first.Urgency, Is.EqualTo(ev1.Urgency));
            Assert.That(first.RequiredSkills, Is.EquivalentTo(ev1.RequiredSkills));
            Assert.That(first.TimeAtEvent, Is.EqualTo("1 hour 30 minutes"));

            Assert.That(second.Id, Is.EqualTo(ev2.Id));
            Assert.That(second.Name, Is.EqualTo(ev2.Name));
            Assert.That(second.Urgency, Is.EqualTo(ev2.Urgency));
            Assert.That(second.RequiredSkills, Is.EquivalentTo(ev2.RequiredSkills));
            Assert.That(second.TimeAtEvent, Is.EqualTo("45 minutes"));
        });
    }
}
