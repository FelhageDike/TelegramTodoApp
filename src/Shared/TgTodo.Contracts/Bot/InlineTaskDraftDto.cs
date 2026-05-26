using TgTodo.Contracts.Enums;

namespace TgTodo.Contracts.Bot;

/// <summary>Черновик inline-задачи (бот ↔ BFF). Хранится в BFF, чтобы пережить рестарт бота.</summary>
public sealed class InlineTaskDraftDto
{
    public string Id { get; set; } = "";
    public long TelegramUserId { get; set; }
    public string AuthorDisplayName { get; set; } = "";
    /// <summary>chat_instance из inline/callback — идентификатор чата, куда ушла карточка.</summary>
    public string? ChatInstance { get; set; }
    public string Title { get; set; } = "";
    public int Points { get; set; }
    public Guid? GroupId { get; set; }
    public TaskScope Scope { get; set; }
    public string ScopeLabel { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateTime ExpiresAt { get; set; }
}
