using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgTodo.AspNetCore.Auth;
using TgTodo.Tasks.Application.Tasks;

namespace TgTodo.Tasks.Api.Controllers;

[Authorize(Policy = TgTodoAuthDefaults.InternalPolicy)]
[ApiController]
[Route("internal/admin")]
public class InternalAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalAdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("task-stats")]
    public async Task<ActionResult<IReadOnlyList<AdminUserTaskStatsDto>>> GetTaskStats(CancellationToken ct)
    {
        var stats = await _mediator.Send(new GetAdminTaskStatsQuery(), ct);
        return Ok(stats);
    }

    [HttpGet("users/{userId:guid}/tasks")]
    public async Task<ActionResult<IReadOnlyList<AdminTaskDto>>> GetUserTasks(Guid userId, CancellationToken ct)
    {
        var tasks = await _mediator.Send(new GetAdminUserTasksQuery(userId), ct);
        return Ok(tasks);
    }
}
