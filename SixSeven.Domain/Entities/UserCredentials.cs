using SixSeven.Domain.Enums;

namespace SixSeven.Domain.Entities;

public class UserCredentials
{
    protected UserCredentials() { }

    public UserCredentials(
        string email,
        string passwordHash,
        UserRole role = UserRole.Volunteer)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password is required.", nameof(passwordHash));

        Id = Guid.NewGuid().ToString("N");
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Notifications = new List<Notification>();
    }

    public string Id { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; } = UserRole.Volunteer;

    public UserProfile? Profile { get; private set; }

    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void AttachProfile(UserProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }
}