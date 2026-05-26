using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TgTodo.Bot.Services;
using TgTodo.Contracts.Enums;

namespace TgTodo.Bot;

public sealed class BotUpdateHandler
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly BotOptions _options;
    private readonly BffClient _bff;
    private readonly UserSessionStore _sessions;
    private readonly InlineDraftStore _drafts;
    private readonly ILogger<BotUpdateHandler> _logger;

    public BotUpdateHandler(
        IOptions<BotOptions> options,
        BffClient bff,
        UserSessionStore sessions,
        InlineDraftStore drafts,
        ILogger<BotUpdateHandler> logger)
    {
        _options = options.Value;
        _bff = bff;
        _sessions = sessions;
        _drafts = drafts;
        _logger = logger;
    }

    public void CleanupDrafts() => _drafts.CleanupExpired();

    public async Task HandleAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message is { } message)
                await HandleMessageAsync(bot, message, ct);
            else if (update.CallbackQuery is { } callback)
                await HandleCallbackAsync(bot, callback, ct);
            else if (update.InlineQuery is { } inline)
                await HandleInlineQueryAsync(bot, inline, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle update {Type}", update.Type);
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        if (message.From is null || string.IsNullOrWhiteSpace(message.Text))
            return;

        var user = message.From;
        var text = message.Text.Trim();
        var chatId = message.Chat.Id;
        var session = _sessions.GetOrCreate(user.Id);

        if (session.PendingCommand == "newgroup" && !text.StartsWith('/'))
        {
            _sessions.ClearPending(user.Id);
            var (group, error) = await _bff.CreateGroupAsync(user.Id, GetDisplayName(user), text, ct);
            if (group is null)
            {
                await bot.SendMessage(chatId, error ?? "Не удалось создать группу", cancellationToken: ct);
                return;
            }

            await bot.SendMessage(chatId,
                $"Группа «{group.Name}» создана.\nКод приглашения: `{group.InviteCode}`",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
            return;
        }

        if (!text.StartsWith('/'))
            return;

        var parts = text.Split(' ', 2, StringSplitOptions.TrimEntries);
        var command = parts[0].Split('@')[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1].Trim() : "";

        switch (command)
        {
            case "/start":
                await SendWelcomeAsync(bot, chatId, ct);
                break;
            case "/app":
                await bot.SendMessage(chatId, "Откройте приложение:", replyMarkup: AppKeyboard(), cancellationToken: ct);
                break;
            case "/help":
                await bot.SendMessage(chatId, HelpText(), cancellationToken: ct);
                break;
            case "/today":
                await SendTodayAsync(bot, chatId, user, session, ct);
                break;
            case "/balance":
                await SendBalanceAsync(bot, chatId, user, session, ct);
                break;
            case "/history":
                await SendHistoryAsync(bot, chatId, user, session, ct);
                break;
            case "/groups":
                await SendGroupsAsync(bot, chatId, user, session, ct);
                break;
            case "/newgroup":
                if (!string.IsNullOrWhiteSpace(args))
                {
                    _sessions.ClearPending(user.Id);
                    var (group, error) = await _bff.CreateGroupAsync(user.Id, GetDisplayName(user), args, ct);
                    await bot.SendMessage(chatId,
                        group is null ? error ?? "Ошибка" : $"Группа «{group.Name}», код: {group.InviteCode}",
                        cancellationToken: ct);
                }
                else
                {
                    _sessions.SetPending(user.Id, "newgroup");
                    await bot.SendMessage(chatId, "Введите название группы следующим сообщением.", cancellationToken: ct);
                }
                break;
            case "/join":
                if (string.IsNullOrWhiteSpace(args))
                {
                    await bot.SendMessage(chatId, "Использование: /join КОД", cancellationToken: ct);
                    break;
                }

                var (joined, joinError) = await _bff.JoinGroupAsync(user.Id, GetDisplayName(user), args.ToUpperInvariant(), ct);
                await bot.SendMessage(chatId,
                    joined is null ? joinError ?? "Ошибка" : $"Вы в группе «{joined.Name}»",
                    cancellationToken: ct);
                break;
            case "/newtask":
                if (string.IsNullOrWhiteSpace(args))
                {
                    await bot.SendMessage(chatId, "Использование: /newtask название [+баллы]\nИли в любом чате: @бот название", cancellationToken: ct);
                    break;
                }

                await CreateTaskFromTextAsync(bot, chatId, user, session, args, ct);
                break;
            case "/context":
                if (args.Equals("personal", StringComparison.OrdinalIgnoreCase) || args.Equals("личные", StringComparison.OrdinalIgnoreCase))
                {
                    _sessions.SetGroup(user.Id, null, null);
                    await bot.SendMessage(chatId, "Контекст: личные задачи", cancellationToken: ct);
                }
                else
                {
                    await bot.SendMessage(chatId, "Использование: /context personal\nИли выберите группу в /groups", cancellationToken: ct);
                }
                break;
            default:
                await bot.SendMessage(chatId, "Неизвестная команда. /help", cancellationToken: ct);
                break;
        }
    }

    private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        if (callback.From is null || callback.Data is null)
            return;

        var data = callback.Data;
        var user = callback.From;
        var chatId = callback.Message?.Chat.Id;
        var messageId = callback.Message?.MessageId;

        if (data.StartsWith("tadd:", StringComparison.Ordinal))
        {
            var draftId = data["tadd:".Length..];
            var draft = _drafts.Get(draftId);
            if (draft is null || draft.TelegramUserId != user.Id)
            {
                await bot.AnswerCallbackQuery(callback.Id, "Черновик устарел", showAlert: true, cancellationToken: ct);
                return;
            }

            var body = _bff.BuildCreateTaskBody(draft.Title, draft.Points, draft.GroupId, draft.StartDate);
            var (task, error) = await _bff.CreateTaskAsync(user.Id, GetDisplayName(user), body, ct);
            if (task is null)
            {
                await bot.AnswerCallbackQuery(callback.Id, error ?? "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            _drafts.Delete(draftId);
            await bot.AnswerCallbackQuery(callback.Id, "Добавлено", cancellationToken: ct);
            if (chatId.HasValue && messageId.HasValue)
            {
                await bot.EditMessageText(chatId.Value, messageId.Value,
                    $"✅ Задача добавлена: {draft.Title} (+{draft.Points})",
                    cancellationToken: ct);
            }
            return;
        }

        if (data.StartsWith("tno:", StringComparison.Ordinal))
        {
            var draftId = data["tno:".Length..];
            _drafts.Delete(draftId);
            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            if (chatId.HasValue && messageId.HasValue)
            {
                await bot.EditMessageText(chatId.Value, messageId.Value, "Задача отклонена", cancellationToken: ct);
            }
            return;
        }

        if (data.StartsWith("done:", StringComparison.Ordinal))
        {
            if (!Guid.TryParse(data["done:".Length..], out var taskId))
            {
                await bot.AnswerCallbackQuery(callback.Id, "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var (task, error) = await _bff.CompleteTaskAsync(user.Id, GetDisplayName(user), taskId, today, ct);
            if (task is null)
            {
                await bot.AnswerCallbackQuery(callback.Id, error ?? "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            await bot.AnswerCallbackQuery(callback.Id, "Выполнено", cancellationToken: ct);
            var session = _sessions.GetOrCreate(user.Id);
            if (chatId.HasValue)
                await SendTodayAsync(bot, chatId.Value, user, session, ct);
            return;
        }

        if (data.StartsWith("ctx:", StringComparison.Ordinal))
        {
            if (data == "ctx:personal")
            {
                _sessions.SetGroup(user.Id, null, null);
                await bot.AnswerCallbackQuery(callback.Id, "Личные задачи", cancellationToken: ct);
            }
            else if (Guid.TryParse(data["ctx:".Length..], out var groupId))
            {
                var groups = await _bff.GetGroupsAsync(user.Id, GetDisplayName(user), ct);
                var g = groups?.FirstOrDefault(x => x.Id == groupId);
                _sessions.SetGroup(user.Id, groupId, g?.Name);
                await bot.AnswerCallbackQuery(callback.Id, g is null ? "Группа" : g.Name, cancellationToken: ct);
            }

            if (chatId.HasValue)
                await SendGroupsAsync(bot, chatId.Value, user, _sessions.GetOrCreate(user.Id), ct);
        }
    }

    private async Task HandleInlineQueryAsync(ITelegramBotClient bot, InlineQuery inline, CancellationToken ct)
    {
        if (inline.From is null)
            return;

        var user = inline.From;
        var parsed = InlineTaskParser.Parse(inline.Query, _options.DefaultPoints);
        if (parsed is null)
        {
            var hint = new InlineQueryResultArticle(
                "hint",
                "Создать задачу",
                new InputTextMessageContent("📋 Введите название после @бота"))
            {
                Description = "Пример: купить молоко +10 #семья"
            };
            await bot.AnswerInlineQuery(inline.Id, new[] { hint }, cacheTime: 0, isPersonal: true, cancellationToken: ct);
            return;
        }

        var session = _sessions.GetOrCreate(user.Id);
        var (groupId, groupName, scopeLabel) = await ResolveScopeAsync(user, session, parsed.GroupTag, ct);
        var scope = groupId.HasValue ? TaskScope.Group : TaskScope.Personal;
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var draftId = Guid.NewGuid().ToString("N")[..12];

        var draft = new InlineTaskDraft
        {
            Id = draftId,
            TelegramUserId = user.Id,
            Title = parsed.Title,
            Points = parsed.Points,
            GroupId = groupId,
            Scope = scope,
            ScopeLabel = scopeLabel,
            StartDate = startDate,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_options.DraftTtlMinutes)
        };
        _drafts.Save(draft);

        var messageText = $"📋 *Задача:* {EscapeMarkdown(parsed.Title)}";
        var result = new InlineQueryResultArticle(
            draftId,
            $"Задача: {parsed.Title}",
            new InputTextMessageContent(messageText) { ParseMode = ParseMode.Markdown })
        {
            Description = $"{scopeLabel} · +{parsed.Points} баллов",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🙋 Добавить себе", $"tadd:{draftId}") },
                new[] { InlineKeyboardButton.WithCallbackData("⭕ Отклонить", $"tno:{draftId}") }
            })
        };

        await bot.AnswerInlineQuery(inline.Id, new[] { result }, cacheTime: 0, isPersonal: true, cancellationToken: ct);
    }

    private async Task CreateTaskFromTextAsync(
        ITelegramBotClient bot,
        long chatId,
        User user,
        UserSession session,
        string args,
        CancellationToken ct)
    {
        var parsed = InlineTaskParser.Parse(args, _options.DefaultPoints);
        if (parsed is null)
        {
            await bot.SendMessage(chatId, "Укажите название задачи", cancellationToken: ct);
            return;
        }

        var (groupId, _, _) = await ResolveScopeAsync(user, session, parsed.GroupTag, ct);
        var body = _bff.BuildCreateTaskBody(parsed.Title, parsed.Points, groupId, DateOnly.FromDateTime(DateTime.UtcNow));
        var (task, error) = await _bff.CreateTaskAsync(user.Id, GetDisplayName(user), body, ct);
        await bot.SendMessage(chatId,
            task is null ? error ?? "Ошибка" : $"✅ Задача создана: {task.Title} (+{task.PointsReward})",
            cancellationToken: ct);
    }

    private async Task SendWelcomeAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        await bot.SendMessage(chatId,
            "TgTodo — задачи и баллы для себя и семьи.\n\n" +
            "• Mini App — полный интерфейс\n" +
            "• В любом чате: `@бот название +10` — карточка с кнопками\n" +
            "• /today — задачи на сегодня\n\n" +
            "/help — все команды",
            parseMode: ParseMode.Markdown,
            replyMarkup: AppKeyboard(),
            cancellationToken: ct);
    }

    private async Task SendTodayAsync(ITelegramBotClient bot, long chatId, User user, UserSession session, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var home = await _bff.GetHomeAsync(user.Id, GetDisplayName(user), session.SelectedGroupId, today, ct);
        if (home is null)
        {
            await bot.SendMessage(chatId, "Не удалось загрузить задачи", cancellationToken: ct);
            return;
        }

        var ctx = session.SelectedGroupName ?? "личные";
        var sb = new StringBuilder($"📋 *Задачи на сегодня* ({ctx})\n\n");
        var buttons = new List<List<InlineKeyboardButton>>();

        if (home.Tasks.Count == 0)
            sb.Append("На сегодня задач нет.");
        else
        {
            var i = 1;
            foreach (var task in home.Tasks.OrderBy(t => t.IsCompletedForPeriod).ThenBy(t => t.Title))
            {
                var mark = task.IsCompletedForPeriod ? "✅" : "⬜";
                sb.AppendLine($"{i}. {mark} {task.Title} (+{task.PointsReward})");
                if (!task.IsCompletedForPeriod)
                {
                    buttons.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData($"✅ {i}", $"done:{task.Id}")
                    });
                }
                i++;
            }
        }

        sb.AppendLine($"\n💰 Личный: {home.Balance.PersonalBalance}");
        if (home.Balance.GroupBalance.HasValue)
            sb.AppendLine($"👥 Группа: {home.Balance.GroupBalance}");

        await bot.SendMessage(chatId, sb.ToString(),
            parseMode: ParseMode.Markdown,
            replyMarkup: buttons.Count > 0 ? new InlineKeyboardMarkup(buttons) : null,
            cancellationToken: ct);
    }

    private async Task SendBalanceAsync(ITelegramBotClient bot, long chatId, User user, UserSession session, CancellationToken ct)
    {
        var balance = await _bff.GetBalanceAsync(user.Id, GetDisplayName(user), session.SelectedGroupId, ct);
        if (balance is null)
        {
            await bot.SendMessage(chatId, "Не удалось загрузить баланс", cancellationToken: ct);
            return;
        }

        var text = $"💰 Личный: *{balance.PersonalBalance}*";
        if (balance.GroupBalance.HasValue)
            text += $"\n👥 Группа ({session.SelectedGroupName}): *{balance.GroupBalance}*";
        else if (session.SelectedGroupId.HasValue)
            text += "\n👥 Группа: 0";

        await bot.SendMessage(chatId, text, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task SendHistoryAsync(ITelegramBotClient bot, long chatId, User user, UserSession session, CancellationToken ct)
    {
        var ledger = await _bff.GetLedgerAsync(user.Id, GetDisplayName(user), session.SelectedGroupId, 10, ct);
        if (ledger is null)
        {
            await bot.SendMessage(chatId, "Не удалось загрузить историю", cancellationToken: ct);
            return;
        }

        if (ledger.Count == 0)
        {
            await bot.SendMessage(chatId, "Пока нет операций", cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder("📜 *История* (последние)\n\n");
        foreach (var e in ledger)
        {
            var sign = e.Delta >= 0 ? "+" : "";
            var when = e.CreatedAt.ToLocalTime().ToString("d MMM HH:mm", Ru);
            sb.AppendLine($"{when}: {sign}{e.Delta} — {FormatReason(e.Reason)}");
        }

        await bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task SendGroupsAsync(ITelegramBotClient bot, long chatId, User user, UserSession session, CancellationToken ct)
    {
        var groups = await _bff.GetGroupsAsync(user.Id, GetDisplayName(user), ct);
        if (groups is null)
        {
            await bot.SendMessage(chatId, "Не удалось загрузить группы", cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder("👥 *Группы*\n\n");
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new() { InlineKeyboardButton.WithCallbackData("Личные задачи", "ctx:personal") }
        };

        if (groups.Count == 0)
            sb.Append("Нет групп. /newgroup Название");
        else
        {
            foreach (var g in groups)
            {
                var active = session.SelectedGroupId == g.Id ? " ✓" : "";
                sb.AppendLine($"• {g.Name}{active} — код `{g.InviteCode}`");
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"Контекст: {g.Name}", $"ctx:{g.Id}")
                });
            }
        }

        var ctx = session.SelectedGroupName ?? "личные";
        sb.AppendLine($"\nТекущий контекст: *{ctx}*");

        await bot.SendMessage(chatId, sb.ToString(),
            parseMode: ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    private async Task<(Guid? GroupId, string? GroupName, string ScopeLabel)> ResolveScopeAsync(
        User user,
        UserSession session,
        string? groupTag,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(groupTag))
        {
            var groups = await _bff.GetGroupsAsync(user.Id, GetDisplayName(user), ct);
            var match = groups?.FirstOrDefault(g =>
                g.Name.Equals(groupTag, StringComparison.OrdinalIgnoreCase) ||
                g.InviteCode.Equals(groupTag, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return (match.Id, match.Name, match.Name);
            return (null, null, "личная");
        }

        if (session.SelectedGroupId.HasValue)
            return (session.SelectedGroupId, session.SelectedGroupName, session.SelectedGroupName ?? "группа");

        return (null, null, "личная");
    }

    private InlineKeyboardMarkup AppKeyboard() =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithWebApp("📱 Открыть TgTodo", new WebAppInfo { Url = _options.MiniAppUrl }) }
        });

    private static string GetDisplayName(User user)
    {
        var name = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        return string.IsNullOrEmpty(name) ? user.Username ?? "User" : name;
    }

    private static string FormatReason(string reason) => reason switch
    {
        "task_complete" => "задача выполнена",
        _ => reason.Replace('_', ' ')
    };

    private static string EscapeMarkdown(string text) =>
        text.Replace("_", "\\_", StringComparison.Ordinal).Replace("*", "\\*", StringComparison.Ordinal);

    private static string HelpText() =>
        """
        *Команды TgTodo*

        /start — приветствие
        /app — Mini App
        /today — задачи на сегодня
        /balance — баллы
        /history — история
        /groups — группы и контекст
        /newgroup — создать группу
        /join КОД — вступить
        /newtask текст — быстрая задача
        /context personal — личные задачи

        *Inline в любом чате:*
        `@бот купить молоко +10 #группа`
        → карточка «Добавить себе» / «Отклонить»
        """;
}
