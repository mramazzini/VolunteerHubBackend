using MediatR;
using Microsoft.AspNetCore.Http;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Authorization;

public record SignupCommand(SignupRequest Request) : IRequest<AuthResponse>;

public sealed class SignupCommandHandler(
    IGenericRepository<UserCredentials> credentialsRepository,
    IGenericRepository<Notification> notificationRepository,
    IEncryptionService encryption,
    IJwtService jwt,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<SignupCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        SignupCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var existing = await credentialsRepository.FindAsync(
            u => u.Email == dto.Email,
            cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var passwordHash = encryption.HashPassword(dto.Password);

        var credentials = new UserCredentials(
            email: dto.Email,
            passwordHash: passwordHash
        );

        credentialsRepository.QueueInsert(credentials, cancellationToken);
        await credentialsRepository.SaveAsync(cancellationToken);

        var notification = new Notification(
            userId: credentials.Id,
            message: "Welcome to Volunteer Hub! 🎉 Thanks for signing up."
        );

        notificationRepository.QueueInsert(notification, cancellationToken);
        await notificationRepository.SaveAsync(cancellationToken);

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

        return new AuthResponse(
            credentials.Id,
            credentials.Email,
            credentials.Role.ToString()
        );
    }
}
