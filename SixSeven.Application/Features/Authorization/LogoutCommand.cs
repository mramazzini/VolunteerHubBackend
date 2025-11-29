using MediatR;
using Microsoft.AspNetCore.Http;

namespace SixSeven.Application.Features.Authorization;

public record LogoutCommand(string UserId) : IRequest<Unit>;

public sealed class LogoutCommandHandler(IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<LogoutCommand, Unit>
{
    public Task<Unit> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext
                   ?? throw new InvalidOperationException("No HttpContext available.");

        http.Response.Cookies.Append(
            "auth_token",
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false, 
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });

        return Task.FromResult(Unit.Value);
    }
}