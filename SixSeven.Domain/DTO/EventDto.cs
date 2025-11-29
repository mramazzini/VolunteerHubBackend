using SixSeven.Domain.Enums;

namespace SixSeven.Domain.DTO;

public class EventDto
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Location { get; set; } = null!;

    public string DateIsoString { get; set; } = null!;

    public EventUrgency Urgency { get; set; }

    public List<VolunteerSkill> RequiredSkills { get; set; } = new();

    public int? Spots { get; set; }
    public bool? IsRemote { get; set; }
}