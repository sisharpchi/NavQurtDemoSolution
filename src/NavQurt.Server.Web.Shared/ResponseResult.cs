using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NavQurt.Server.Web.Shared
{
    public record ResponseResult : IResponseResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Error { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ErrorCode { get; init; }
        public virtual bool Success => string.IsNullOrWhiteSpace(Error);

        public static ResponseResult CreateSuccess() => new();
        public static ResponseResult CreateError(string error) => new() { Error = error };
        public static ResponseResult CreateError(string error, int errorCode) => new() { Error = error, ErrorCode = errorCode };
    }

    public record ResponseResult<T> : ResponseResult
    {
        [MemberNotNullWhen(true, nameof(Value))]
        public override bool Success => base.Success;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Value { get; init; }

        public static ResponseResult<T> CreateSuccess(T result) => new() { Value = result };
        public new static ResponseResult<T> CreateError(string error, int errorCode) => new() { Error = error, ErrorCode = errorCode };
        public new static ResponseResult<T> CreateError(string error) => new() { Error = error };
    }
    public record ResponseResult<T, TError> : ResponseResult
    {
        [MemberNotNullWhen(true, nameof(Value))]
        public override bool Success => base.Success;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Value { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TError? ErrorData { get; set; }

        public static ResponseResult<T, TError> CreateSuccess(T result) => new() { Value = result };
        public new static ResponseResult<T, TError> CreateError(string error) => new() { Error = error };
        public static ResponseResult<T, TError> CreateError(string error, TError? errorData) => new() { ErrorData = errorData, Error = error };
        public static ResponseResult<T, TError> CreateError(string error, int errorCode, TError? errorData) => new() { ErrorData = errorData, Error = error, ErrorCode = errorCode };
    }
}
