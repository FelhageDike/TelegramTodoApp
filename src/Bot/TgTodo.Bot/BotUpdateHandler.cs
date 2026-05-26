using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TgTodo.Bot.Services;
using TgTodo.Contracts;
using TgTodo.Contracts.Bot;
using TgTodo.Contracts.Enums;

namespace TgTodo.Bot;

public sealed class BotUpdateHandler
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly BotOptions _options;
    private readonly BffClient _bff;
    private readonly UserSessionStore _sessions;
    private readonly InlineChatPeerStore _chatPeers;
    private readonly ILogger<BotUpdateHandler> _logger;
    private readonly object _botUsernameLock = new();
    private string? _cachedBotUsername;

    public BotUpdateHandler(
        IOptions<BotOptions> options,
        BffClient bff,
        UserSessionStore sessions,
        InlineChatPeerStore chatPeers,
        ILogger<BotUpdateHandler> logger)
    {
        _options = options.Value;
        _bff = bff;
        _sessions = sessions;
        _chatPeers = chatPeers;
        _logger = logger;
    }

    public async Task CleanupDraftsAsync(CancellationToken ct)
    {
        try
        {
            await _bff.PruneInlineDraftsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prune inline drafts failed");
        }
    }

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

            await SendGroupCreatedAsync(bot, chatId, group, ct);
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
                await HandleStartAsync(bot, chatId, user, args, ct);
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
                    if (group is null)
                        await bot.SendMessage(chatId, error ?? "Ошибка", cancellationToken: ct);
                    else
                        await SendGroupCreatedAsync(bot, chatId, group, ct);
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

        if (data.StartsWith("tstart:", StringComparison.Ordinal))
        {
            await HandleInlineOpenCallbackAsync(bot, callback, user, data, ct);
            return;
        }

        if (data.StartsWith("tadd:", StringComparison.Ordinal))
        {
            await HandleInlineAddCallbackAsync(bot, callback, user, data, ct);
            return;
        }

        if (data.StartsWith("tno:", StringComparison.Ordinal))
        {
            var draftId = data["tno:".Length..];
            await _bff.DeleteInlineDraftAsync(draftId, ct);
            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            await BotCallbackMessages.TryEditTextAsync(bot, callback, "Задача отклонена", _logger, cancellationToken: ct);
            return;
        }

        if (data.StartsWith("done:", StringComparison.Ordinal))
        {
            if (!Guid.TryParse(data["done:".Length..], out var taskId))
            {
                await bot.AnswerCallbackQuery(callback.Id, "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            var today = await TodayForUserAsync(user, ct);
            var (task, error) = await _bff.CompleteTaskAsync(user.Id, GetDisplayName(user), taskId, today, ct);
            if (task is null)
            {
                await bot.AnswerCallbackQuery(callback.Id, error ?? "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            await bot.AnswerCallbackQuery(callback.Id, "Выполнено", cancellationToken: ct);
            var session = _sessions.GetOrCreate(user.Id);
            if (callback.Message?.Chat.Id is { } doneChatId)
                await SendTodayAsync(bot, doneChatId, user, session, ct);
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

            if (callback.Message?.Chat.Id is { } ctxChatId)
                await SendGroupsAsync(bot, ctxChatId, user, _sessions.GetOrCreate(user.Id), ct);
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
        var startDate = await TodayForUserAsync(user, ct);
        var draftId = Guid.NewGuid().ToString("N")[..12];

        _chatPeers.Remember(null, draftId, user.Id);

        var draftDto = new InlineTaskDraftDto
        {
            Id = draftId,
            TelegramUserId = user.Id,
            AuthorDisplayName = GetDisplayName(user),
            Title = parsed.Title,
            Points = parsed.Points,
            GroupId = groupId,
            Scope = scope,
            ScopeLabel = scopeLabel,
            StartDate = startDate,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_options.DraftTtlMinutes)
        };

        try
        {
            await _bff.SaveInlineDraftAsync(draftDto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save inline draft to BFF");
            var err = new InlineQueryResultArticle(
                "draft-err",
                "Не удалось сохранить черновик",
                new InputTextMessageContent("Проверьте связь с сервером и попробуйте снова."))
            {
                Description = "BFF недоступен или неверный ключ бота"
            };
            await bot.AnswerInlineQuery(inline.Id, new[] { err }, cacheTime: 0, isPersonal: true, cancellationToken: ct);
            return;
        }

        var messageText = $"📋 *Задача:* {EscapeMarkdown(parsed.Title)}";
        var result = new InlineQueryResultArticle(
            draftId,
            $"Задача: {parsed.Title}",
            new InputTextMessageContent(messageText) { ParseMode = ParseMode.Markdown })
        {
            Description = $"{scopeLabel} · +{parsed.Points} · нажмите «Добавить задачу»",
            ReplyMarkup = BuildInlineInitialKeyboard(draftId)
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
        var body = _bff.BuildCreateTaskBody(parsed.Title, parsed.Points, groupId, await TodayForUserAsync(user, ct));
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
            "• В любом чате: `@бот название +10` — сначала «Добавить задачу», потом личная или в общей группе\n" +
            "• /today — задачи на сегодня\n" +
            "• Ссылка-приглашение в группу (из /groups) — вступление без команды /join\n\n" +
            "/help — все команды",
            parseMode: ParseMode.Markdown,
            replyMarkup: AppKeyboard(),
            cancellationToken: ct);
    }

    private async Task HandleStartAsync(ITelegramBotClient bot, long chatId, User user, string args, CancellationToken ct)
    {
        var payload = args.Trim();
        if (payload.StartsWith("join_", StringComparison.OrdinalIgnoreCase))
        {
            var code = payload["join_".Length..].Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                await bot.SendMessage(chatId, "В ссылке нет кода. Попросите приглашение ещё раз или введите: /join КОД", cancellationToken: ct);
                return;
            }

            var (joined, joinError) = await _bff.JoinGroupAsync(user.Id, GetDisplayName(user), code.ToUpperInvariant(), ct);
            await bot.SendMessage(chatId,
                joined is null ? joinError ?? "Не удалось вступить в группу" : $"Вы в группе «{joined.Name}»",
                cancellationToken: ct);
            return;
        }

        await SendWelcomeAsync(bot, chatId, ct);
    }

    private async Task SendGroupCreatedAsync(ITelegramBotClient bot, long chatId, GroupDto group, CancellationToken ct)
    {
        var username = await GetBotUsernameAsync(bot, ct);
        var sb = new StringBuilder();
        sb.AppendLine($"Группа «{group.Name}» создана.");
        sb.AppendLine($"Код приглашения: `{group.InviteCode}`");
        if (!string.IsNullOrEmpty(username))
            sb.AppendLine($"Ссылка для вступления: https://t.me/{username}?start=join_{group.InviteCode}");

        await bot.SendMessage(chatId, sb.ToString().TrimEnd(), parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task<string?> GetBotUsernameAsync(ITelegramBotClient bot, CancellationToken ct)
    {
        lock (_botUsernameLock)
        {
            if (!string.IsNullOrEmpty(_cachedBotUsername))
                return _cachedBotUsername;
        }

        var me = await bot.GetMe(ct);
        var u = me.Username ?? "";
        lock (_botUsernameLock)
        {
            _cachedBotUsername = u;
        }

        return string.IsNullOrEmpty(u) ? null : u;
    }

    private async Task SendTodayAsync(ITelegramBotClient bot, long chatId, User user, UserSession session, CancellationToken ct)
    {
        var today = await TodayForUserAsync(user, ct);
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

        var botUsername = await GetBotUsernameAsync(bot, ct);
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
                if (!string.IsNullOrEmpty(botUsername))
                    sb.AppendLine($"  приглашение: `https://t.me/{botUsername}?start=join_{g.InviteCode}`");
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

    private async Task HandleInlineOpenCallbackAsync(
        ITelegramBotClient bot,
        CallbackQuery callback,
        User user,
        string data,
        CancellationToken ct)
    {
        var draftId = data["tstart:".Length..].Trim();
        if (string.IsNullOrEmpty(draftId))
        {
            await bot.AnswerCallbackQuery(callback.Id, "Ошибка", showAlert: true, cancellationToken: ct);
            return;
        }

        var draft = await _bff.GetInlineDraftAsync(draftId, ct);
        if (draft is null)
        {
            await bot.AnswerCallbackQuery(callback.Id,
                "Черновик не найден или истёк. Снова введите @бот и текст задачи.",
                showAlert: true, cancellationToken: ct);
            return;
        }

        var chatInstance = callback.ChatInstance ?? draft.ChatInstance;
        _chatPeers.Remember(chatInstance, draftId, user.Id);
        _chatPeers.Remember(chatInstance, draftId, draft.TelegramUserId);

        var authorName = string.IsNullOrWhiteSpace(draft.AuthorDisplayName) ? "User" : draft.AuthorDisplayName;
        var peerId = user.Id != draft.TelegramUserId
            ? user.Id
            : _chatPeers.TryGetSingleOtherPeer(chatInstance, draftId, draft.TelegramUserId);

        IReadOnlyList<GroupDto> shared = [];
        if (peerId is not null)
            shared = await GetSharedGroupsAsync(
                draft.TelegramUserId, authorName, peerId.Value, GetDisplayName(user), ct);

        var markup = shared.Count > 0
            ? BuildInlineAfterOpenKeyboard(draftId, personalAndGroup: true)
            : BuildInlineAfterOpenKeyboard(draftId, personalAndGroup: false);

        await BotCallbackMessages.TryEditReplyMarkupAsync(bot, callback, markup, _logger, ct);
        await bot.AnswerCallbackQuery(callback.Id,
            shared.Count > 0
                ? "Выберите: личная или в общей группе TgTodo"
                : "Добавьте личную задачу",
            cancellationToken: ct);
    }

    private static InlineKeyboardMarkup BuildInlineInitialKeyboard(string draftId) =>
        new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📌 Добавить задачу", $"tstart:{draftId}") },
            new[] { InlineKeyboardButton.WithCallbackData("⭕ Отклонить", $"tno:{draftId}") }
        });

    /// <summary>После «Добавить задачу»: две кнопки или одна «Личная задача».</summary>
    private static InlineKeyboardMarkup BuildInlineAfterOpenKeyboard(string draftId, bool personalAndGroup) =>
        personalAndGroup
            ? new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🙋 Личная", $"tadd:p:{draftId}"),
                    InlineKeyboardButton.WithCallbackData("👥 В группе", $"tadd:g:{draftId}")
                },
                new[] { InlineKeyboardButton.WithCallbackData("⭕ Отклонить", $"tno:{draftId}") }
            })
            : new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🙋 Личная задача", $"tadd:p:{draftId}") },
                new[] { InlineKeyboardButton.WithCallbackData("⭕ Отклонить", $"tno:{draftId}") }
            });

    private async Task HandleInlineAddCallbackAsync(
        ITelegramBotClient bot,
        CallbackQuery callback,
        User user,
        string data,
        CancellationToken ct)
    {
        // tadd:p:{id} — личная; tadd:g:{id} — в общей группе; tadd:g:{id}:{groupId} — выбранная группа;
        // tadd:{id} — совместимость: личная
        var payload = data["tadd:".Length..];
        var parts = payload.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            await bot.AnswerCallbackQuery(callback.Id, "Ошибка", showAlert: true, cancellationToken: ct);
            return;
        }

        var mode = parts[0];
        string draftId;
        Guid? pickGroupId = null;

        if (mode is "p" or "g")
        {
            if (parts.Length < 2)
            {
                await bot.AnswerCallbackQuery(callback.Id, "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            draftId = parts[1];
            if (mode == "g" && parts.Length >= 3 && Guid.TryParse(parts[2], out var gid))
                pickGroupId = gid;
        }
        else
        {
            mode = "p";
            draftId = parts[0];
        }

        var draft = await _bff.GetInlineDraftAsync(draftId, ct);
        if (draft is null)
        {
            await bot.AnswerCallbackQuery(callback.Id,
                "Черновик не найден или истёк. Снова введите @бот и текст задачи.",
                showAlert: true, cancellationToken: ct);
            return;
        }

        var authorName = string.IsNullOrWhiteSpace(draft.AuthorDisplayName) ? "User" : draft.AuthorDisplayName;
        var clickerName = GetDisplayName(user);
        var today = await TodayForUserAsync(user, ct);

        if (mode == "p")
        {
            var body = _bff.BuildCreateTaskBody(draft.Title, draft.Points, null, today);
            var (task, error) = await _bff.CreateTaskAsync(user.Id, clickerName, body, ct);
            if (task is null)
            {
                await bot.AnswerCallbackQuery(callback.Id, error ?? "Ошибка", showAlert: true, cancellationToken: ct);
                return;
            }

            await _bff.DeleteInlineDraftAsync(draftId, ct);
            await bot.AnswerCallbackQuery(callback.Id, "Добавлено", cancellationToken: ct);
            await BotCallbackMessages.TryEditTextAsync(
                bot, callback, $"✅ Личная задача: {draft.Title} (+{draft.Points})", _logger, cancellationToken: ct);
            return;
        }

        var shared = await GetSharedGroupsAsync(draft.TelegramUserId, authorName, user.Id, clickerName, ct);
        if (shared.Count == 0)
        {
            await bot.AnswerCallbackQuery(callback.Id,
                "Нет общей группы TgTodo. Создайте группу в приложении и пригласите собеседника.",
                showAlert: true, cancellationToken: ct);
            return;
        }

        if (!pickGroupId.HasValue)
        {
            if (draft.GroupId is { } hinted && shared.Any(g => g.Id == hinted))
                pickGroupId = hinted;
            else if (shared.Count == 1)
                pickGroupId = shared[0].Id;
        }

        if (!pickGroupId.HasValue)
        {
            await bot.AnswerCallbackQuery(callback.Id, "Выберите группу", cancellationToken: ct);
            var pickerText = $"📋 {draft.Title}\nВыберите группу:";
            await BotCallbackMessages.TryEditTextAsync(
                bot, callback, pickerText, _logger, BuildGroupPickerKeyboard(draftId, shared), cancellationToken: ct);
            return;
        }

        var group = shared.FirstOrDefault(g => g.Id == pickGroupId);
        if (group is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, "Вы не в этой группе", showAlert: true, cancellationToken: ct);
            return;
        }

        var userId = await _bff.GetUserIdAsync(user.Id, clickerName, ct);
        if (userId is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, "Не удалось определить профиль", showAlert: true, cancellationToken: ct);
            return;
        }

        var groupBody = _bff.BuildCreateTaskBody(draft.Title, draft.Points, group.Id, today, userId);
        var (groupTask, groupError) = await _bff.CreateTaskAsync(user.Id, clickerName, groupBody, ct);
        if (groupTask is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, groupError ?? "Ошибка", showAlert: true, cancellationToken: ct);
            return;
        }

        await _bff.DeleteInlineDraftAsync(draftId, ct);
        await bot.AnswerCallbackQuery(callback.Id, "Назначено", cancellationToken: ct);
        await BotCallbackMessages.TryEditTextAsync(
            bot, callback,
            $"✅ В группе «{group.Name}» на вас: {draft.Title} (+{draft.Points})",
            _logger, cancellationToken: ct);
    }

    private static InlineKeyboardMarkup BuildGroupPickerKeyboard(string draftId, IReadOnlyList<GroupDto> groups)
    {
        var rows = groups
            .Take(6)
            .Select(g => new[] { InlineKeyboardButton.WithCallbackData(g.Name, $"tadd:g:{draftId}:{g.Id}") })
            .ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⭕ Отмена", $"tno:{draftId}") });
        return new InlineKeyboardMarkup(rows);
    }

    private async Task<List<GroupDto>> GetSharedGroupsAsync(
        long authorTelegramId,
        string authorDisplayName,
        long clickerTelegramId,
        string clickerDisplayName,
        CancellationToken ct)
    {
        if (authorTelegramId == clickerTelegramId)
            return [];

        var authorGroups = await _bff.GetGroupsAsync(authorTelegramId, authorDisplayName, ct) ?? [];
        var clickerGroups = await _bff.GetGroupsAsync(clickerTelegramId, clickerDisplayName, ct) ?? [];
        var clickerIds = clickerGroups.Select(g => g.Id).ToHashSet();
        return authorGroups.Where(g => clickerIds.Contains(g.Id)).ToList();
    }

    private async Task<DateOnly> TodayForUserAsync(User user, CancellationToken ct)
    {
        var tz = await _bff.GetUserTimezoneAsync(user.Id, GetDisplayName(user), ct);
        return TimeZoneCalendar.Today(tz);
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
        Ссылка `https://t.me/ИМЯ_БОТА?start=join_КОД` — то же, одним нажатием из чата
        /newtask текст — быстрая задача
        /context personal — личные задачи

        *Inline в любом чате:*
        `@бот купить молоко +10 #группа`
        → «Добавить задачу», затем «Личная» и «В группе» (если с собеседником есть общая группа TgTodo) или одна «Личная задача»
        """;
}
