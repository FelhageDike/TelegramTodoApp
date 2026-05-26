using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgTodo.Gamification.Application.Balance;

namespace TgTodo.Gamification.Api.Controllers;

[ApiController]
[Route("api")]
public class BalanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public BalanceController(IMediator mediator) => _mediator = mediator;

    private Guid? CurrentUserId =>
        Guid.TryParse(Request.Headers["X-User-Id"], out var id) ? id : null;

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance([FromQuery] Guid? groupId, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var balance = await _mediator.Send(new GetBalanceQuery(userId, groupId), ct);
        return Ok(balance);
    }

    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyList<LedgerEntryDto>>> GetLedger(
        [FromQuery] Guid? groupId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var ledger = await _mediator.Send(new GetLedgerQuery(userId, groupId, take), ct);
        return Ok(ledger);
    }
}
