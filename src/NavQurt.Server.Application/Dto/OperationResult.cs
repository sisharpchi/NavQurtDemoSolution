using System.Linq;

namespace NavQurt.Server.Application.Dto;

public enum OperationStatus
{
    Success,
    NotFound,
    Invalid,
    Conflict,
    Forbidden,
    Error
}

public class OperationResult<T>
{
    private OperationResult(OperationStatus status, T? data, IReadOnlyList<string> errors)
    {
        Status = status;
        Data = data;
        Errors = errors;
    }

    public OperationStatus Status { get; }

    public bool Succeeded => Status == OperationStatus.Success;

    public T? Data { get; }

    public IReadOnlyList<string> Errors { get; }

    public static OperationResult<T> Success(T data) =>
        new(OperationStatus.Success, data, Array.Empty<string>());

    public static OperationResult<T> NotFound(params string[] errors) =>
        Failure(OperationStatus.NotFound, errors);

    public static OperationResult<T> Invalid(params string[] errors) =>
        Failure(OperationStatus.Invalid, errors);

    public static OperationResult<T> Conflict(params string[] errors) =>
        Failure(OperationStatus.Conflict, errors);

    public static OperationResult<T> Forbidden(params string[] errors) =>
        Failure(OperationStatus.Forbidden, errors);

    public static OperationResult<T> Error(params string[] errors) =>
        Failure(OperationStatus.Error, errors);

    public static OperationResult<T> Failure(OperationStatus status, IEnumerable<string> errors) =>
        new(status, default, NormalizeErrors(errors));

    private static IReadOnlyList<string> NormalizeErrors(IEnumerable<string> errors)
    {
        if (errors is IReadOnlyList<string> readOnly)
        {
            return readOnly;
        }

        return errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray() ?? Array.Empty<string>();
    }
}
