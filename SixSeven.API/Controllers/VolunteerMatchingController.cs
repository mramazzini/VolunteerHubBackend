using MediatR;
using Microsoft.AspNetCore.Mvc;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Matching;

namespace SixSeven.Controllers;

[ApiController]
[Route("matching")]
public sealed class VolunteerMatchingController(IMediator mediator) : ControllerBase
{
    [HttpGet("volunteers")]
    public async Task<ActionResult<IReadOnlyList<VolunteerDto>>> GetVolunteers(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetVolunteersForMatchingQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEventsForMatchingQuery(), cancellationToken);
        return Ok(result);
    }
}