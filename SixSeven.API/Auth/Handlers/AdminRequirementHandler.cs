using Microsoft.AspNetCore.Authorization;
using SixSeven.Application.Authorization;
using SixSeven.Application.Interfaces.ReadStore;
using SixSeven.Domain.Enums;

namespace SixSeven.Auth.Handlers;

public class AdminRequirementHandler(IUserReadStore userReadStore) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value; 
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var role = await userReadStore.GetRoleByIdAsync(userId);

        if (role == UserRole.Admin)
        {
            context.Succeed(requirement);
        }
    }
}
