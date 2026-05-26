namespace TgTodo.BuildingBlocks.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
