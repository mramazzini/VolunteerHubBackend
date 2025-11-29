namespace SixSeven.Application.Interfaces.Services;

public interface IEncryptionService
{
    /// <summary>
    /// Hashes a plain-text password using bcrypt.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain-text password against a previously hashed password.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}