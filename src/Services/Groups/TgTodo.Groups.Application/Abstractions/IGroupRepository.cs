using TgTodo.Groups.Domain.Entities;

namespace TgTodo.Groups.Application.Abstractions;

public interface IGroupRepository
{
    Task<FamilyGroup?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FamilyGroup?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyGroup>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task AddAsync(FamilyGroup group, CancellationToken ct = default);
    Task AddMemberAsync(GroupMember member, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task DeleteGroupAsync(Guid groupId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
