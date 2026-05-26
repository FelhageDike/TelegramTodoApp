using TgTodo.Contracts.Enums;

namespace TgTodo.Groups.Application.Groups;

public record GroupDto(Guid Id, string Name, string InviteCode, GroupRole MyRole);
public record GroupMemberDto(Guid UserId, GroupRole Role, DateTime JoinedAt);
