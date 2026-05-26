using MassTransit;
using TgTodo.Contracts.Enums;
using TgTodo.Contracts.Events;
using TgTodo.Gamification.Application.Abstractions;
using TgTodo.Gamification.Domain.Entities;

namespace TgTodo.Gamification.Application.Consumers;

public class TaskCompletedConsumer : IConsumer<TaskCompletedIntegrationEvent>
{
    private readonly IGamificationRepository _repo;

    public TaskCompletedConsumer(IGamificationRepository repo) => _repo = repo;

    public async Task Consume(ConsumeContext<TaskCompletedIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        if (await _repo.IsEventProcessedAsync(message.EventId, ct))
            return;

        var account = await GetOrCreateAccountAsync(message, ct);
        account.ApplyDelta(message.PointsReward);

        await _repo.AddLedgerEntryAsync(
            PointLedgerEntry.Create(account.Id, message.PointsReward, "task_complete", message.TaskId), ct);
        await _repo.MarkEventProcessedAsync(message.EventId, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task<Account> GetOrCreateAccountAsync(TaskCompletedIntegrationEvent message, CancellationToken ct)
    {
        if (message.Scope == TaskScope.Group)
        {
            if (message.GroupId is null)
                throw new InvalidOperationException("GroupId required for group scope.");

            var groupAccount = await _repo.GetGroupAccountAsync(message.GroupId.Value, ct);
            if (groupAccount is not null) return groupAccount;

            groupAccount = Account.CreateGroup(message.GroupId.Value);
            await _repo.AddAccountAsync(groupAccount, ct);
            return groupAccount;
        }

        var personal = await _repo.GetPersonalAccountAsync(message.CompletedByUserId, ct);
        if (personal is not null) return personal;

        personal = Account.CreatePersonal(message.CompletedByUserId);
        await _repo.AddAccountAsync(personal, ct);
        return personal;
    }
}
