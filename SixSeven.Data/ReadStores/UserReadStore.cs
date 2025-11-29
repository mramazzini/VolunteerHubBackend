using Microsoft.EntityFrameworkCore;
using SixSeven.Application.Interfaces.ReadStore;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Data.ReadStores;

public class UserReadStore(AppDbContext dbContext) : IUserReadStore
{
    public async Task<UserCredentials?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return await dbContext.UserCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserCredentials?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await dbContext.UserCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<UserRole?> GetRoleByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await GetByIdAsync(id, ct);
        return user?.Role;
    }

    public async Task<UserRole?> GetRoleByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await GetByEmailAsync(email, ct);
        return user?.Role;
    }
}