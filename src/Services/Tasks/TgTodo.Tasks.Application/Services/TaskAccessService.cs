using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Contracts.Enums;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Application.Services;

public class TaskAccessService
{
    private readonly IGroupsClient _groups;

    public TaskAccessService(IGroupsClient groups) => _groups = groups;

    public async Task EnsureCanViewAsync(TodoTask task, Guid userId, CancellationToken ct)
    {
        if (task.Scope == TaskScope.Group)
        {
            if (task.GroupId is null || !await _groups.IsMemberAsync(task.GroupId.Value, userId, ct))
                throw new ForbiddenException("Not allowed to view this task.");
            return;
        }

        switch (task.PersonalVisibility)
        {
            case PersonalTaskVisibility.Private:
                if (task.OwnerUserId != userId && task.CreatedByUserId != userId &&
                    task.AssignedToUserId != userId)
                    throw new ForbiddenException("Not allowed to view this task.");
                break;
            case PersonalTaskVisibility.AssigneeOnly:
                if (task.OwnerUserId != userId && task.AssignedToUserId != userId &&
                    task.CreatedByUserId != userId)
                    throw new ForbiddenException("Not allowed to view this task.");
                break;
            case PersonalTaskVisibility.GroupMembers:
                if (task.GroupId is null || !await _groups.IsMemberAsync(task.GroupId.Value, userId, ct))
                    throw new ForbiddenException("Not allowed to view this task.");
                break;
        }
    }

    public async Task EnsureCanCompleteAsync(TodoTask task, Guid userId, CancellationToken ct)
    {
        await EnsureCanViewAsync(task, userId, ct);

        if (task.Scope == TaskScope.Group && task.AssignedToUserId.HasValue &&
            task.AssignedToUserId != userId)
            throw new ForbiddenException("Task assigned to another member.");

        if (task.Scope == TaskScope.Personal && task.AssignedToUserId.HasValue &&
            task.AssignedToUserId != userId && task.OwnerUserId != userId)
            throw new ForbiddenException("Task assigned to another user.");
    }
}
