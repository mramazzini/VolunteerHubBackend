using SixSeven.Domain.Enums;

namespace SixSeven.Application.Dtos;

public sealed class UserDto
{
    public string Id { get; init; } = null!;

    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;

    public string AddressOne { get; init; } = null!;
    public string? AddressTwo { get; init; }
    public string City { get; init; } = null!;
    public string State { get; init; } = null!;
    public string ZipCode { get; init; } = null!;

    public IReadOnlyList<VolunteerSkill> Skills { get; init; } = [];

    public string Preferences { get; init; } = string.Empty;

    public IReadOnlyList<string> Availability { get; init; } = [];

    public UserRole Role { get; init; }
}