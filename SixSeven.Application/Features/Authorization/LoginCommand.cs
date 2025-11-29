using MediatR;
using Microsoft.AspNetCore.Http;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Authorization;

public record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;

public sealed class LoginCommandHandler(
    IGenericRepository<UserCredentials> credentialsRepository,
    IEncryptionService encryption,
    IJwtService jwt,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var credentials = await credentialsRepository.FindAsync(
            u => u.Email == dto.Email,
            cancellationToken);

        if (credentials is null ||
            !encryption.VerifyPassword(dto.Password, credentials.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var token = jwt.GenerateToken(
            credentials.Id,
            credentials.Email,
            credentials.Role.ToString());

        var http = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HttpContext available.");

        http.Response.Cookies.Append(
            "auth_token",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false, 
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(60)
            });

        return new AuthResponse(credentials.Id,
            credentials.Email,
            credentials.Role.ToString());
    }
}