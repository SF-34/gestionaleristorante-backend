using GestionaleRistorante.Common;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Success<T>(T data, int statusCode = StatusCodes.Status200OK)
    {
        return StatusCode(statusCode, ApiResponse<T>.Success(data, statusCode));
    }

    protected IActionResult SuccessNoData(int statusCode = StatusCodes.Status200OK)
    {
        return StatusCode(statusCode, ApiResponse<object?>.Success(null, statusCode));
    }
}
