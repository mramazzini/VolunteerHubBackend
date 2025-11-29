using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Features.Users;

public record GetCurrentUserQuery(string UserId) : IRequest<UserDto?>;

public sealed class GetCurrentUserQueryHandler(
    IGenericRepository<UserCredentials> credentialsRepo,
    IGenericRepository<UserProfile> profileRepo)
    : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var credentials = await credentialsRepo.FindAsync(
            c => c.Id == request.UserId,
            cancellationToken);

        if (credentials is null)
            return null;

        var profile = await profileRepo.FindAsync(
            p => p.UserCredentialsId == request.UserId,
            cancellationToken);

        return new UserDto
        {
            Id = credentials.Id,
            Email = credentials.Email,
            Role = credentials.Role,

            FirstName = profile?.FirstName,
            LastName = profile?.LastName,

            AddressOne = profile?.AddressOne,
            AddressTwo = profile?.AddressTwo,
            City = profile?.City,
            State = profile?.State,
            ZipCode = profile?.ZipCode,

            Skills = profile?.Skills ?? new(),
            Preferences = profile?.Preferences ?? string.Empty,
            Availability = profile?.Availability ?? new()
        };
    }
}