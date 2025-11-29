using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixSeven.Application.Features.Notifications;

namespace SixSeven.Controllers;

[ApiController]
[Route("")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var notifications = await mediator.Send(
            new GetNotificationsForUserQuery(userId));

        return Ok(notifications);
        
    }
    
    [HttpPost("notifications/mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var updatedCount = await mediator.Send(
            new MarkAllNotificationsReadCommand(userId),
            cancellationToken);

        return Ok(new { updated = updatedCount });
    }
}