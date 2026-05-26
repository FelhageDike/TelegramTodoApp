using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgTodo.Groups.Application.Groups;

namespace TgTodo.Groups.Api.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GroupsController(IMediator mediator) => _mediator = mediator;

    private Guid? CurrentUserId =>
        Guid.TryParse(Request.Headers["X-User-Id"], out var id) ? id : null;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> GetGroups(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var groups = await _mediator.Send(new GetGroupsQuery(userId), ct);
        return Ok(groups);
    }

    [HttpPost]
    public async Task<ActionResult<GroupDto>> Create([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var group = await _mediator.Send(new CreateGroupCommand(userId, request.Name), ct);
        return Ok(group);
    }

    [HttpPost("join")]
    public async Task<ActionResult<GroupDto>> Join([FromBody] JoinGroupRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var group = await _mediator.Send(new JoinGroupCommand(userId, request.InviteCode), ct);
        return Ok(group);
    }

    [HttpGet("{groupId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<GroupMemberDto>>> GetMembers(Guid groupId, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var members = await _mediator.Send(new GetMembersQuery(userId, groupId), ct);
        return Ok(members);
    }

    [HttpGet("{groupId:guid}/membership")]
    public async Task<ActionResult<bool>> CheckMembership(Guid groupId, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var isMember = await _mediator.Send(new CheckMembershipQuery(userId, groupId), ct);
        return Ok(isMember);
    }

    [HttpPost("{groupId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        await _mediator.Send(new LeaveGroupCommand(userId, groupId), ct);
        return NoContent();
    }

    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> Delete(Guid groupId, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        await _mediator.Send(new DeleteGroupCommand(userId, groupId), ct);
        return NoContent();
    }
}

public record CreateGroupRequest(string Name);
public record JoinGroupRequest(string InviteCode);
