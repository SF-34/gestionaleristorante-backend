namespace GestionaleRistorante.Common;

public sealed class ApiResponse<T>
{
    public T? Data { get; init; }

    public ApiError? Error { get; init; }

    public int StatusCode { get; init; }

    public static ApiResponse<T> Success(T? data, int statusCode)
    {
        return new ApiResponse<T>
        {
            Data = data,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> Failure(string code, string message, int statusCode, object? details = null)
    {
        return new ApiResponse<T>
        {
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            },
            StatusCode = statusCode
        };
    }
}

public sealed class ApiError
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public object? Details { get; init; }
}
