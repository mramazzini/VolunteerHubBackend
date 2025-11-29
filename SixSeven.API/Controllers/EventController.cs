using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixSeven.Application.Features.Events;
using SixSeven.Application.Features.VolunteerHistory;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Models;
using EventDto = SixSeven.Application.Dtos.EventDto;
using VolunteerHistory = SixSeven.Domain.Entities.VolunteerHistory;

namespace SixSeven.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IMediator mediator, IGenericRepository<VolunteerHistory> _history) : ControllerBase
{
    [HttpGet("upcoming")]
    [AllowAnonymous] 
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetUpcoming()
    {
        var result = await mediator.Send(new GetUpcomingEventsQuery());
        return Ok(result);
    }
    
    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<VolunteerHistoryDto>>> GetVolunteerHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await mediator.Send(new GetVolunteerHistoryForUserQuery(userId));

        return Ok(result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")] 
    public async Task<ActionResult<EventDto>> UpdateEvent(
        [FromBody] UpsertEventRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpsertEventCommand(
            Id: request.Id,
            Name: request.Name,
            Description: request.Description,
            Location: request.Location,
            DateUtc: request.DateUtc,
            Urgency: request.Urgency,
            RequiredSkills: request.RequiredSkills
        );

        var result = await mediator.Send(cmd, cancellationToken);
        return Ok(result);
    }
    
    [HttpPost("{eventId}/assign-volunteer")]
    public async Task<ActionResult<AssignVolunteerToEventResult>> AssignVolunteer(
        string eventId,
        [FromBody] AssignVolunteerRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VolunteerId))
            return BadRequest("VolunteerId is required.");

        var result = await mediator.Send(
            new AssignVolunteerToEventCommand(
                EventId: eventId,
                VolunteerId: request.VolunteerId,
                DurationMinutes: request.DurationMinutes),
            cancellationToken);

        if (result is null)
            return NotFound(); 

        return Ok(result);
    }
    
    [HttpGet("{eventId}/assignments")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetAssignedVolunteers(
        string eventId,
        CancellationToken cancellationToken)
    {
        var histories = await _history.GetAsync(
            h => h.EventId == eventId,
            cancellationToken);

        var volunteerIds = histories
            .Select(h => h.UserId)
            .Distinct()
            .ToList();

        return Ok(volunteerIds);
    }
}