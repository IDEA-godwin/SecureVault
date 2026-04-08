using SecureVault.Application.Common;
using SecureVault.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Web.Infrastructure;

/// <summary>
/// Converts well-known application exceptions into RFC 9110-compliant <see cref="ProblemDetails"/> responses,
/// mapping <see cref="ValidationException"/> → 400, <see cref="NotFoundException"/> → 404,
/// <see cref="UnauthorizedAccessException"/> → 401, and <see cref="ForbiddenAccessException"/> → 403.
/// Unrecognised exceptions are not handled and fall through to the default middleware.
/// </summary>
public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, new ValidationProblemDetails(ve.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
            }),
            AppNotFoundException ne => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = ExtractErrorCode(ne.Message) ?? "The specified resource was not found.",
                Detail = ne.Message
            }),
            ArgumentNullException ane => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = ExtractErrorCode(ane.Message) ?? "The specified resource was not found.",
                Detail = ane.Message ?? "The requested entity was not found."
            }),
            ArgumentException ae => (StatusCodes.Status400BadRequest, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ExtractErrorCode(ae.Message) ?? ErrorCodes.OperationFailed,
                Detail = ae.Message
            }),
            InvalidOperationException ioe => (StatusCodes.Status400BadRequest, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ExtractErrorCode(ioe.Message) ?? ErrorCodes.OperationFailed,
                Detail = ioe.Message
            }),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
            }),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
            }),
            _ => (-1, null)
        };

        if (problemDetails is null) return false;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static string? ExtractErrorCode(string? message)
    {
        if (string.IsNullOrEmpty(message)) return null;

        if (message.StartsWith("[") && message.Contains("]"))
        {
            var endBracket = message.IndexOf("]");
            var code = message.Substring(1, endBracket - 1);
            if (!string.IsNullOrWhiteSpace(code))
            {
                return code;
            }
        }

        return null;
    }
}
