namespace TgTodo.Groups.Application.Groups;

public static class InviteCodeNormalizer
{
    /// <summary>
    /// Оставляет только латинские буквы и цифры (как при генерации кода), uppercase.
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return new string(code
            .Where(c => char.IsAsciiLetterOrDigit(c))
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
