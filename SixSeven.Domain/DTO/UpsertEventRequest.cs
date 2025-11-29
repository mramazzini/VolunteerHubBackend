namespace SixSeven.Domain.DTO;


public sealed class UpsertEventRequest
{

    public string? Id { get; init; }

    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Location { get; init; } = null!;

    public DateTime DateUtc { get; init; }

    public string Urgency { get; init; } = null!;

    public IReadOnlyList<string> RequiredSkills { get; init; } = Array.Empty<string>();
}