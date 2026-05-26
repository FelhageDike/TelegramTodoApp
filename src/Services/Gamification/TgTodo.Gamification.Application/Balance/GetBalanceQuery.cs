using MediatR;
using TgTodo.Gamification.Application.Abstractions;

namespace TgTodo.Gamification.Application.Balance;

public record GetBalanceQuery(Guid UserId, Guid? GroupId) : IRequest<BalanceDto>;

public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, BalanceDto>
{
    private readonly IGamificationRepository _repo;

    public GetBalanceQueryHandler(IGamificationRepository repo) => _repo = repo;

    public async Task<BalanceDto> Handle(GetBalanceQuery request, CancellationToken ct)
    {
        var personal = await _repo.GetPersonalAccountAsync(request.UserId, ct);
        if (personal is null)
        {
            personal = Domain.Entities.Account.CreatePersonal(request.UserId);
            await _repo.AddAccountAsync(personal, ct);
            await _repo.SaveChangesAsync(ct);
        }

        int? groupBalance = null;
        if (request.GroupId.HasValue)
        {
            var groupAccount = await _repo.GetGroupAccountAsync(request.GroupId.Value, ct);
            if (groupAccount is null)
            {
                groupAccount = Domain.Entities.Account.CreateGroup(request.GroupId.Value);
                await _repo.AddAccountAsync(groupAccount, ct);
                await _repo.SaveChangesAsync(ct);
            }
            groupBalance = groupAccount.Balance;
        }

        return new BalanceDto(personal.Balance, groupBalance);
    }
}
