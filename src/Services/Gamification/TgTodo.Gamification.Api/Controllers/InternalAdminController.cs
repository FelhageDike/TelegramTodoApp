using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgTodo.AspNetCore.Auth;
using TgTodo.Gamification.Application.Balance;

namespace TgTodo.Gamification.Api.Controllers;

[Authorize(Policy = TgTodoAuthDefaults.InternalPolicy)]
[ApiController]
[Route("internal/admin")]
public class InternalAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalAdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<PersonalBalanceDto>>> GetAllBalances(CancellationToken ct)
    {
        var balances = await _mediator.Send(new GetAllPersonalBalancesQuery(), ct);
        return Ok(balances);
    }
}
