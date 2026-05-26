using Microsoft.EntityFrameworkCore;
using TgTodo.Groups.Application.Abstractions;
using TgTodo.Groups.Domain.Entities;
using TgTodo.Groups.Infrastructure.Persistence;

namespace TgTodo.Groups.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly GroupsDbContext _db;

    public GroupRepository(GroupsDbContext db) => _db = db;

    public Task<FamilyGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<FamilyGroup?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default) =>
        _db.Groups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.InviteCode == inviteCode, ct);

    public async Task<IReadOnlyList<FamilyGroup>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Groups
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);

    public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) =>
        _db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, ct);

    public Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) =>
        _db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, ct);

    public async Task AddAsync(FamilyGroup group, CancellationToken ct = default) =>
        await _db.Groups.AddAsync(group, ct);

    public async Task AddMemberAsync(GroupMember member, CancellationToken ct = default) =>
        await _db.GroupMembers.AddAsync(member, ct);

    public async Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, ct);
        if (member is not null)
            _db.GroupMembers.Remove(member);
    }

    public async Task DeleteGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var members = await _db.GroupMembers.Where(m => m.GroupId == groupId).ToListAsync(ct);
        _db.GroupMembers.RemoveRange(members);
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is not null)
            _db.Groups.Remove(group);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
