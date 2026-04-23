using GestionaleRistorante.Common;
using GestionaleRistorante.Core.Exceptions;

namespace GestionaleRistorante.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException appException)
        {
            await WriteErrorAsync(context, appException.StatusCode, appException.Code, appException.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception.");
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object?>.Failure(code, message, statusCode);
        return context.Response.WriteAsJsonAsync(payload);
    }
}
