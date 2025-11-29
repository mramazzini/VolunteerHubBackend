using SixSeven.Domain.Enums;

namespace SixSeven.Domain.Entities;

public class UserProfile
{
    protected UserProfile() { }

    public UserProfile(
        string userCredentialsId,
        string firstName,
        string lastName,
        string addressOne,
        string city,
        string state,
        string zipCode,
        string? preferences = null,
        IEnumerable<VolunteerSkill>? skills = null,
        IEnumerable<string>? otherSkills = null,
        IEnumerable<string>? availability = null)
    {
        if (string.IsNullOrWhiteSpace(userCredentialsId)) throw new ArgumentException("UserCredentialsId is required.", nameof(userCredentialsId));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(addressOne)) throw new ArgumentException("AddressOne is required.", nameof(addressOne));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(zipCode)) throw new ArgumentException("ZipCode is required.", nameof(zipCode));

        Id = userCredentialsId;
        UserCredentialsId = userCredentialsId;

        FirstName = firstName;
        LastName = lastName;
        AddressOne = addressOne;
        City = city;
        State = state;
        ZipCode = zipCode;

        Preferences = preferences ?? string.Empty;
        Skills = skills?.Distinct().ToList() ?? new List<VolunteerSkill>();
        Availability = availability?.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList() ?? new List<string>();
    }

    public string Id { get; private set; } = null!;

    public string UserCredentialsId { get; private set; } = null!;

    public UserCredentials Credentials { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public string AddressOne { get; private set; } = null!;
    public string? AddressTwo { get; private set; }
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string ZipCode { get; private set; } = null!;

    public List<VolunteerSkill> Skills { get; private set; } = new();

    public string Preferences { get; private set; } = string.Empty;

    public List<string> Availability { get; private set; } = new();

    public void UpdateProfile(
        string firstName,
        string lastName,
        string addressOne,
        string? addressTwo,
        string city,
        string state,
        string zipCode,
        string? preferences,
        IEnumerable<VolunteerSkill>? skills,
        IEnumerable<string>? availability)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(addressOne)) throw new ArgumentException("AddressOne is required.", nameof(addressOne));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(zipCode)) throw new ArgumentException("ZipCode is required.", nameof(zipCode));

        FirstName = firstName;
        LastName = lastName;
        AddressOne = addressOne;
        AddressTwo = addressTwo;
        City = city;
        State = state;
        ZipCode = zipCode;
        Preferences = preferences ?? string.Empty;

        Skills = skills?.Distinct().ToList() ?? new List<VolunteerSkill>();
        Availability = availability?.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList() ?? new List<string>();
    }
}
