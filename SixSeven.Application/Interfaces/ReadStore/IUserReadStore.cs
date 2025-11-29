using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Interfaces.ReadStore;

public interface IUserReadStore
{
    Task<UserCredentials?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserCredentials?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<UserRole?> GetRoleByIdAsync(string id, CancellationToken ct = default);
    Task<UserRole?> GetRoleByEmailAsync(string email, CancellationToken ct = default);
}