using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgTodo.Identity.Application.Users;

namespace TgTodo.Identity.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        if (!Guid.TryParse(Request.Headers["X-User-Id"], out var userId))
            return Unauthorized();

        var user = await _mediator.Send(new GetUserQuery(userId), ct);
        return Ok(user);
    }

    [HttpPatch("me/timezone")]
    public async Task<ActionResult<UserDto>> UpdateTimezone([FromBody] UpdateTimezoneRequest body, CancellationToken ct)
    {
        if (!Guid.TryParse(Request.Headers["X-User-Id"], out var userId))
            return Unauthorized();

        var user = await _mediator.Send(new UpdateUserTimezoneCommand(userId, body.Timezone), ct);
        return Ok(user);
    }
}

public record UpdateTimezoneRequest(string Timezone);

[ApiController]
[Route("internal/users")]
public class InternalUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalUsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost("ensure")]
    public async Task<ActionResult<UserDto>> Ensure([FromBody] EnsureUserRequest request, CancellationToken ct)
    {
        var user = await _mediator.Send(
            new EnsureUserCommand(request.TelegramId, request.DisplayName, request.Timezone), ct);
        return Ok(user);
    }

    [HttpPost("by-ids")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetByIds([FromBody] GetUsersByIdsRequest request, CancellationToken ct)
    {
        var users = await _mediator.Send(new GetUsersByIdsQuery(request.UserIds), ct);
        return Ok(users);
    }
}

public record EnsureUserRequest(long TelegramId, string DisplayName, string Timezone);
public record GetUsersByIdsRequest(IReadOnlyList<Guid> UserIds);
