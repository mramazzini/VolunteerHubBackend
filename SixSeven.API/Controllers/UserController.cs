using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Users;
using SixSeven.Domain.DTO;

namespace SixSeven.Controllers;

[ApiController]
[Route("")]
[Authorize] 
public class UserController(ISender mediator) : ControllerBase
{
    [HttpGet("user")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await mediator.Send(new GetCurrentUserQuery(userId));

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPut("user")]
    public async Task<ActionResult<UserDto>> UpdateUser([FromBody] UpdateUserRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var updated = await mediator.Send(new UpdateUserCommand(userId, request));

        return Ok(updated);
    }
}