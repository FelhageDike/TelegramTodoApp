using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgTodo.AspNetCore.Auth;
using TgTodo.Gamification.Application.Balance;

namespace TgTodo.Gamification.Api.Controllers;

[ApiController]
[Route("api")]
public class BalanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public BalanceController(IMediator mediator) => _mediator = mediator;

    [Authorize(Policy = TgTodoAuthDefaults.GroupMemberOptionalPolicy)]
    [HttpGet("balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance([FromQuery] Guid? groupId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var balance = await _mediator.Send(new GetBalanceQuery(userId, groupId), ct);
        return Ok(balance);
    }

    [Authorize(Policy = TgTodoAuthDefaults.GroupMemberOptionalPolicy)]
    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyList<LedgerEntryDto>>> GetLedger(
        [FromQuery] Guid? groupId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var ledger = await _mediator.Send(new GetLedgerQuery(userId, groupId, take), ct);
        return Ok(ledger);
    }
}
