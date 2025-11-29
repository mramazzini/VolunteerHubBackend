using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SixSeven.Application.Mappers;
using SixSeven.Application.Dtos;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Mappers;

[TestFixture]
public class EventDtoMapperTests
{
    private static Event CreateEvent(
        string id = "event-1",
        string name = "Test Event",
        string description = "Desc",
        string location = "Hall A",
        DateTime? dateUtc = null,
        EventUrgency urgency = EventUrgency.Medium,
        IReadOnlyList<VolunteerSkill>? skills = null)
    {
        var ev = new Event(
            name,
            description,
            location,
            dateUtc ?? new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            urgency,
            skills ?? new[] { VolunteerSkill.Cooking });

        typeof(Event).GetProperty(nameof(Event.Id))!
            .SetValue(ev, id);

        return ev;
    }

    [Test]
    public void ToDto_MapsAllFieldsCorrectly()
    {
        var ev = CreateEvent(
            id: "event-123",
            name: "Food Drive",
            description: "Help cook meals",
            location: "Kitchen",
            dateUtc: new DateTime(2025, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.High,
            skills: new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving });

        var dto = ev.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(ev.Id));
            Assert.That(dto.Name, Is.EqualTo(ev.Name));
            Assert.That(dto.Description, Is.EqualTo(ev.Description));
            Assert.That(dto.Location, Is.EqualTo(ev.Location));
            Assert.That(dto.DateIsoString, Is.EqualTo(ev.DateUtc.ToString("O")));
            Assert.That(dto.Urgency, Is.EqualTo(ev.Urgency));
            Assert.That(dto.RequiredSkills, Is.EquivalentTo(ev.RequiredSkills));
        });
    }

    [Test]
    public void ToDtos_MapsMultipleEntities()
    {
        var ev1 = CreateEvent(id: "e1", name: "Event 1");
        var ev2 = CreateEvent(id: "e2", name: "Event 2");

        var result = new[] { ev1, ev2 }.ToDtos();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Select(d => d.Id), Is.EqualTo(new[] { "e1", "e2" }));
    }

    [Test]
    public void ToDto_NullEntity_Throws()
    {
        Event? ev = null;

        Assert.Throws<ArgumentNullException>(() => ev!.ToDto());
    }

    [Test]
    public void ToDtos_NullCollection_Throws()
    {
        IEnumerable<Event>? events = null;

        Assert.Throws<ArgumentNullException>(() => events!.ToDtos());
    }
}
