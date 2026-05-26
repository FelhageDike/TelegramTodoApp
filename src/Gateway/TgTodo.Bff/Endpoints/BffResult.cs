using TgTodo.Bff.Clients;

namespace TgTodo.Bff.Endpoints;

internal static class BffResult
{
    public static async Task<IResult> From(Func<Task> action)
    {
        try
        {
            await action();
            return Results.NoContent();
        }
        catch (ApiClientException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: (int)ex.StatusCode);
        }
    }

    public static async Task<IResult> From<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (ApiClientException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: (int)ex.StatusCode);
        }
    }
}
