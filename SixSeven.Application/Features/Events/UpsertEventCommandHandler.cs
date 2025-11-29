using MediatR;
using SixSeven.Application.Dtos;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Mappers;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Features.Events;

public sealed record UpsertEventCommand(
    string? Id,
    string Name,
    string Description,
    string Location,
    DateTime DateUtc,
    string Urgency,
    IReadOnlyCollection<string> RequiredSkills
) : IRequest<EventDto>;

public sealed class UpsertEventCommandHandler(
    IGenericRepository<Event> eventRepo
) : IRequestHandler<UpsertEventCommand, EventDto>
{
    public async Task<EventDto> Handle(
        UpsertEventCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EventUrgency>(request.Urgency, ignoreCase: true, out var urgency))
        {
            throw new ArgumentException(
                $"Invalid urgency value '{request.Urgency}'. Expected one of: {string.Join(", ", Enum.GetNames<EventUrgency>())}.");
        }

        var parsedSkills = new List<VolunteerSkill>();

        foreach (var skillStr in request.RequiredSkills ?? Array.Empty<string>())
        {
            var trimmed = skillStr?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (!Enum.TryParse<VolunteerSkill>(trimmed, ignoreCase: true, out var skill))
            {
                throw new ArgumentException(
                    $"Invalid skill value '{skillStr}'. Expected one of: {string.Join(", ", Enum.GetNames<VolunteerSkill>())}.");
            }

            parsedSkills.Add(skill);
        }

        Event entity;

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            entity = new Event(
                name: request.Name,
                description: request.Description,
                location: request.Location,
                dateUtc: request.DateUtc,
                urgency: urgency,
                requiredSkills: parsedSkills
            );

            eventRepo.QueueInsert(entity, cancellationToken);
        }
        else
        {
            entity = await eventRepo.FindAsync(
                e => e.Id == request.Id,
                cancellationToken);

            if (entity is null)
                throw new InvalidOperationException($"Event with id '{request.Id}' not found.");

            entity.UpdateDetails(
                name: request.Name,
                description: request.Description,
                location: request.Location,
                urgency: urgency);

            entity.Reschedule(request.DateUtc);

            entity.SetRequiredSkills(parsedSkills);
        }

        await eventRepo.SaveAsync(cancellationToken);

        return entity.ToDto();
    }
}