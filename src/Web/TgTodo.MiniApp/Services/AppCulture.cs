using System.Globalization;

namespace TgTodo.MiniApp.Services;

internal static class AppCulture
{
    private static readonly Lazy<CultureInfo> RuLazy = new(GetRussianOrInvariant);

    public static CultureInfo Ru => RuLazy.Value;

    private static CultureInfo GetRussianOrInvariant()
    {
        try
        {
            return CultureInfo.GetCultureInfo("ru-RU");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
