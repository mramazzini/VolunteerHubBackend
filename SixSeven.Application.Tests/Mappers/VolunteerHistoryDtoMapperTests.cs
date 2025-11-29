using System;
using System.Collections.Generic;
using NUnit.Framework;
using SixSeven.Application.Mappers;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Mappers;

[TestFixture]
public class VolunteerHistoryDtoMapperTests
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

    private static VolunteerHistory CreateHistory(
        string id = "hist-1",
        string userId = "user-1",
        string eventId = "event-1",
        DateTime? dateUtc = null,
        int durationMinutes = 90)
    {
        var h = new VolunteerHistory(
            userId,
            eventId,
            dateUtc ?? new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            durationMinutes);

        typeof(VolunteerHistory).GetProperty(nameof(VolunteerHistory.Id))!
            .SetValue(h, id);

        return h;
    }

    [Test]
    public void ToDto_MapsAllFields()
    {
        var ev = CreateEvent(
            id: "event-123",
            name: "Food Drive",
            description: "Pack boxes",
            location: "Main Hall",
            dateUtc: new DateTime(2025, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.High,
            skills: new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving });

        var history = CreateHistory(
            id: "hist-9",
            userId: "user-1",
            eventId: ev.Id,
            dateUtc: new DateTime(2025, 2, 1, 11, 0, 0, DateTimeKind.Utc),
            durationMinutes: 135);

        var dto = history.ToDto(ev);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(ev.Id));
            Assert.That(dto.Name, Is.EqualTo(ev.Name));
            Assert.That(dto.Description, Is.EqualTo(ev.Description));
            Assert.That(dto.Location, Is.EqualTo(ev.Location));
            Assert.That(dto.DateIsoString, Is.EqualTo(history.DateUtc.ToString("O")));
            Assert.That(dto.Urgency, Is.EqualTo(ev.Urgency));
            Assert.That(dto.RequiredSkills, Is.EquivalentTo(ev.RequiredSkills));
            Assert.That(dto.TimeAtEvent, Is.EqualTo("2 hours 15 minutes"));
        });
    }


    [TestCase(1, "1 minute")]
    [TestCase(2, "2 minutes")]
    [TestCase(59, "59 minutes")]
    [TestCase(60, "1 hour")]
    [TestCase(120, "2 hours")]
    [TestCase(61, "1 hour 1 minute")]
    [TestCase(62, "1 hour 2 minutes")]
    [TestCase(125, "2 hours 5 minutes")]
    public void ToDto_FormatsDurationCorrectly(int durationMinutes, string expected)
    {
        var ev = CreateEvent();
        var history = CreateHistory(durationMinutes: durationMinutes);

        var dto = history.ToDto(ev);

        Assert.That(dto.TimeAtEvent, Is.EqualTo(expected));
    }

    [Test]
    public void ToDto_NullHistory_Throws()
    {
        VolunteerHistory? history = null;
        var ev = CreateEvent();

        Assert.Throws<ArgumentNullException>(() => VolunteerHistoryDtoMapper.ToDto(history!, ev));
    }

    [Test]
    public void ToDto_NullEvent_Throws()
    {
        var history = CreateHistory();
        Event? ev = null;

        Assert.Throws<ArgumentNullException>(() => VolunteerHistoryDtoMapper.ToDto(history, ev!));
    }
}
