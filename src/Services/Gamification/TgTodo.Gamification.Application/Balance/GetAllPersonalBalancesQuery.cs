using MediatR;
using TgTodo.Gamification.Application.Abstractions;

namespace TgTodo.Gamification.Application.Balance;

public record PersonalBalanceDto(Guid UserId, int Balance);

public record GetAllPersonalBalancesQuery : IRequest<IReadOnlyList<PersonalBalanceDto>>;

public class GetAllPersonalBalancesQueryHandler : IRequestHandler<GetAllPersonalBalancesQuery, IReadOnlyList<PersonalBalanceDto>>
{
    private readonly IGamificationRepository _repository;

    public GetAllPersonalBalancesQueryHandler(IGamificationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<PersonalBalanceDto>> Handle(GetAllPersonalBalancesQuery request, CancellationToken ct)
    {
        var accounts = await _repository.GetAllPersonalAccountsAsync(ct);
        return accounts
            .Where(a => a.UserId.HasValue)
            .Select(a => new PersonalBalanceDto(a.UserId!.Value, a.Balance))
            .ToList();
    }
}
