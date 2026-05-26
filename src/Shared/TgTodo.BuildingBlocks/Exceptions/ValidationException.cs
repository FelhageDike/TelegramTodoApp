namespace TgTodo.BuildingBlocks.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
