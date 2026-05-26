using MediatR;
using TgTodo.Contracts.Enums;
using TgTodo.Gamification.Application.Abstractions;

namespace TgTodo.Gamification.Application.Balance;

public record GetLedgerQuery(Guid UserId, Guid? GroupId, int Take = 50) : IRequest<IReadOnlyList<LedgerEntryDto>>;

public class GetLedgerQueryHandler : IRequestHandler<GetLedgerQuery, IReadOnlyList<LedgerEntryDto>>
{
    private readonly IGamificationRepository _repo;

    public GetLedgerQueryHandler(IGamificationRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<LedgerEntryDto>> Handle(GetLedgerQuery request, CancellationToken ct)
    {
        var account = request.GroupId.HasValue
            ? await _repo.GetGroupAccountAsync(request.GroupId.Value, ct)
            : await _repo.GetPersonalAccountAsync(request.UserId, ct);

        if (account is null)
            return Array.Empty<LedgerEntryDto>();

        var entries = await _repo.GetLedgerAsync(account.Id, request.Take, ct);
        return entries.Select(e => new LedgerEntryDto(e.Delta, e.Reason, e.ReferenceId, e.CreatedAt)).ToList();
    }
}
