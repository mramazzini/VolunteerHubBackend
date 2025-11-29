using SixSeven.Domain.Enums;

namespace SixSeven.Domain.Entities;

public class Event
{
    protected Event() { }

    public Event(
        string name,
        string description,
        string location,
        DateTime dateUtc,
        EventUrgency urgency,
        IEnumerable<VolunteerSkill> requiredSkills)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(location)) throw new ArgumentException("Location is required.", nameof(location));

        Id = Guid.NewGuid().ToString("N");

        Name = name;
        Description = description;
        Location = location;
        DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);
        Urgency = urgency;
        RequiredSkills = requiredSkills?.Distinct().ToList() ?? new List<VolunteerSkill>();
    }

    public string Id { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Location { get; private set; } = null!;

    public DateTime DateUtc { get; private set; }

    public List<VolunteerSkill> RequiredSkills { get; private set; } = new();

    public EventUrgency Urgency { get; private set; }

    public void Reschedule(DateTime newDateUtc)
    {
        DateUtc = DateTime.SpecifyKind(newDateUtc, DateTimeKind.Utc);
    }

    public void UpdateDetails(string name, string description, string location, EventUrgency urgency)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(location)) throw new ArgumentException("Location is required.", nameof(location));

        Name = name;
        Description = description;
        Location = location;
        Urgency = urgency;
    }

    public void SetRequiredSkills(IEnumerable<VolunteerSkill> skills)
    {
        RequiredSkills = skills?.Distinct().ToList() ?? new List<VolunteerSkill>();
    }
}
