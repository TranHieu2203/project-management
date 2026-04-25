namespace ProjectManagement.Shared.Domain.Results;

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, string error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public new static Result<T> Failure(string error) => new(false, default, error);
}
