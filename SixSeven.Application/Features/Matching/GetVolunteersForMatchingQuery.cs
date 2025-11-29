using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Features.Matching;


public sealed record GetVolunteersForMatchingQuery
    : IRequest<IReadOnlyList<VolunteerDto>>;
    

public sealed class GetVolunteersForMatchingHandler(IGenericRepository<UserProfile> users)
    : IRequestHandler<GetVolunteersForMatchingQuery, IReadOnlyList<VolunteerDto>>
{
    public async Task<IReadOnlyList<VolunteerDto>> Handle(
        GetVolunteersForMatchingQuery request,
        CancellationToken cancellationToken)
    {
        var volunteers = await users.GetAsync(
            u => u.Credentials.Role == UserRole.Volunteer,
            cancellationToken);

        return volunteers
            .Select(u => new VolunteerDto(
                Id: u.Id,
                Name: u.FirstName + " " + u.LastName,
                Skills: u.Skills,                     
                Availability: u.Availability))
            .ToList();
    }
}
