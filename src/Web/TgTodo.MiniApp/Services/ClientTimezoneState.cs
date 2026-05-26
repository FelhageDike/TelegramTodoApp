namespace TgTodo.MiniApp.Services;

/// <summary>Текущий IANA-пояс для заголовка <c>X-Client-Timezone</c> на запросах к BFF.</summary>
public class ClientTimezoneState
{
    public string? Current { get; set; }
}
