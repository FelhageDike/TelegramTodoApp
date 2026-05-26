using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgTodo.Bot;

/// <summary>
/// Сообщения из inline-режима (@бот) приходят с <see cref="CallbackQuery.InlineMessageId"/>, не с <see cref="CallbackQuery.Message"/>.
/// </summary>
internal static class BotCallbackMessages
{
    public static async Task TryEditTextAsync(
        ITelegramBotClient bot,
        CallbackQuery callback,
        string text,
        ILogger logger,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrEmpty(callback.InlineMessageId))
            {
                await bot.EditMessageText(
                    callback.InlineMessageId,
                    text,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
                return;
            }

            if (callback.Message is { } message)
            {
                await bot.EditMessageText(
                    message.Chat.Id,
                    message.MessageId,
                    text,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to edit message after callback (inlineMessageId={HasInline}, message={HasMessage})",
                !string.IsNullOrEmpty(callback.InlineMessageId),
                callback.Message is not null);
        }
    }

    public static async Task TryEditReplyMarkupAsync(
        ITelegramBotClient bot,
        CallbackQuery callback,
        InlineKeyboardMarkup? replyMarkup,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrEmpty(callback.InlineMessageId))
            {
                await bot.EditMessageReplyMarkup(
                    callback.InlineMessageId,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
                return;
            }

            if (callback.Message is { } message)
            {
                await bot.EditMessageReplyMarkup(
                    message.Chat.Id,
                    message.MessageId,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to edit inline reply markup");
        }
    }
}
