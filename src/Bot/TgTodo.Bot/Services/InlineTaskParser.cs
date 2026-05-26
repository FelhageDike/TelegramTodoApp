using System.Text.RegularExpressions;

namespace TgTodo.Bot.Services;

public sealed record ParsedInlineTask(string Title, int Points, string? GroupTag);

public static class InlineTaskParser
{
    private static readonly Regex Pattern = new(
        @"^(?<title>.+?)(?:\s+\+(?<points>\d{1,4}))?(?:\s+#(?<group>[^\s#]+))?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static ParsedInlineTask? Parse(string? query, int defaultPoints)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var text = query.Trim();
        if (text.Length > 200)
            text = text[..200];

        var match = Pattern.Match(text);
        if (!match.Success)
            return new ParsedInlineTask(text, defaultPoints, null);

        var title = match.Groups["title"].Value.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var points = defaultPoints;
        if (match.Groups["points"].Success && int.TryParse(match.Groups["points"].Value, out var p))
            points = Math.Clamp(p, 1, 1000);

        var groupTag = match.Groups["group"].Success ? match.Groups["group"].Value.Trim() : null;
        return new ParsedInlineTask(title, points, string.IsNullOrEmpty(groupTag) ? null : groupTag);
    }
}
