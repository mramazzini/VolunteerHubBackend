using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Features.Users;

public record UpdateUserCommand(
    string UserId,
    UpdateUserRequest Request
) : IRequest<UserDto>;

public sealed class UpdateUserCommandHandler(
    IGenericRepository<UserCredentials> credentialsRepo,
    IGenericRepository<UserProfile> profileRepo)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var credentials = await credentialsRepo.FindAsync(
            c => c.Id == request.UserId,
            cancellationToken);

        if (credentials is null)
            throw new InvalidOperationException("User not found.");

        var profile = await profileRepo.FindAsync(
            p => p.UserCredentialsId == request.UserId,
            cancellationToken);
        if (profile is null)
        {
            if (dto.FirstName is null ||
                dto.LastName is null ||
                dto.AddressOne is null ||
                dto.City is null ||
                dto.State is null ||
                dto.ZipCode is null)
            {
                throw new InvalidOperationException(
                    "Cannot create profile: missing required fields.");
            }

            profile = new UserProfile(
                userCredentialsId: credentials.Id,
                firstName: dto.FirstName,
                lastName: dto.LastName,
                addressOne: dto.AddressOne,
                city: dto.City,
                state: dto.State,
                zipCode: dto.ZipCode,
                preferences: dto.Preferences,
                skills: dto.Skills is not null
                    ? ParseSkills(dto.Skills)
                    : null,
                availability: dto.Availability
            );

            profileRepo.QueueInsert(profile, cancellationToken);
        }

        else
        {
            var firstName = dto.FirstName ?? profile.FirstName;
            var lastName = dto.LastName ?? profile.LastName;
            var addressOne = dto.AddressOne ?? profile.AddressOne;
            var addressTwo = dto.AddressTwo ?? profile.AddressTwo;
            var city = dto.City ?? profile.City;
            var state = dto.State ?? profile.State;
            var zipCode = dto.ZipCode ?? profile.ZipCode;

            var skills =
                dto.Skills is not null
                    ? ParseSkills(dto.Skills)
                    : profile.Skills;
            var availability = dto.Availability ?? profile.Availability;

            profile.UpdateProfile(
                firstName: firstName,
                lastName: lastName,
                addressOne: addressOne,
                addressTwo: addressTwo,
                city: city,
                state: state,
                zipCode: zipCode,
                preferences: dto.Preferences ?? profile.Preferences,
                skills: skills,
                availability: availability
            );
        }

        await profileRepo.SaveAsync(cancellationToken);

        return new UserDto
        {
            Id = credentials.Id,
            Email = credentials.Email,
            Role = credentials.Role,

            FirstName = profile.FirstName,
            LastName = profile.LastName,

            AddressOne = profile.AddressOne,
            AddressTwo = profile.AddressTwo,
            City = profile.City,
            State = profile.State,
            ZipCode = profile.ZipCode,

            Skills = profile.Skills,
            Preferences = profile.Preferences,
            Availability = profile.Availability
        };
    }
    
    private static List<VolunteerSkill> ParseSkills(IEnumerable<string>? skills)
    {
        if (skills is null)
            return new List<VolunteerSkill>();

        var result = new List<VolunteerSkill>();

        foreach (var s in skills)
        {
            if (Enum.TryParse<VolunteerSkill>(s, ignoreCase: true, out var parsed))
            {
                result.Add(parsed);
            }
            else
            {
                throw new InvalidOperationException($"Invalid skill: '{s}'");
            }
        }

        return result;
    }

}
