namespace ProjectManagement.Shared.Domain.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, object currentState, string currentETag)
        : base(message)
    {
        CurrentState = currentState;
        CurrentETag = currentETag;
    }

    public object? CurrentState { get; }
    public string? CurrentETag { get; }
}
