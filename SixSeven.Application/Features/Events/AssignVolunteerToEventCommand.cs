using MediatR;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Application.Features.Events;


public sealed record AssignVolunteerToEventCommand(
    string EventId,
    string VolunteerId,
    int DurationMinutes = 60 
) : IRequest<AssignVolunteerToEventResult?>;

public sealed class AssignVolunteerToEventHandler(
    IGenericRepository<Event> events,
    IGenericRepository<UserCredentials> users,
    IGenericRepository<Domain.Entities.VolunteerHistory> history,
    IGenericRepository<Notification> notifications)
    : IRequestHandler<AssignVolunteerToEventCommand, AssignVolunteerToEventResult?>
{
    public async Task<AssignVolunteerToEventResult?> Handle(
        AssignVolunteerToEventCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await events.FindAsync(
            e => e.Id == request.EventId,
            cancellationToken);

        if (@event is null)
            return null;

        var volunteer = await users.FindAsync(
            u => u.Id == request.VolunteerId && u.Role == UserRole.Volunteer,
            cancellationToken);

        if (volunteer is null)
            return null;

        var existing = await history.FindAsync(
            h => h.EventId == request.EventId &&
                 h.UserId == request.VolunteerId,
            cancellationToken);

        if (existing is not null)
        {
            return new AssignVolunteerToEventResult(
                VolunteerHistoryId: existing.Id,
                EventId: existing.EventId,
                VolunteerId: existing.UserId,
                DateUtc: existing.DateUtc,
                DurationMinutes: existing.DurationMinutes);
        }

        var history1 = new Domain.Entities.VolunteerHistory(
            userId: request.VolunteerId,
            eventId: request.EventId,
            dateUtc: @event.DateUtc,
            durationMinutes: request.DurationMinutes);

        history.QueueInsert(history1, cancellationToken);
        
        var notification = new Notification(
            userId: volunteer.Id,
            message: $"You’ve been assigned to the event “{@event.Name}” on {@event.DateUtc:MMMM d, yyyy}."
        );

        notifications.QueueInsert(notification, cancellationToken);

        
        await history.SaveAsync(cancellationToken);

        return new AssignVolunteerToEventResult(
            VolunteerHistoryId: history1.Id,
            EventId: history1.EventId,
            VolunteerId: history1.UserId,
            DateUtc: history1.DateUtc,
            DurationMinutes: history1.DurationMinutes);
    }
}