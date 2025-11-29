using SixSeven.Domain.Enums;

namespace SixSeven.Domain.DTO;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? AddressOne { get; set; }
    public string? AddressTwo { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public IEnumerable<string>? Skills { get; init; }
    public string? Preferences { get; set; }
    public IReadOnlyList<string>? Availability { get; set; }
    
    
}