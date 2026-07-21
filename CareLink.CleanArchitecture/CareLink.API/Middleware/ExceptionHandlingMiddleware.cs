using CareLink.Application.Common.Exceptions;

namespace CareLink.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    // Maps known Application-layer exceptions to their HTTP status code.
    // Anything not listed here falls through to a generic 500 response.
    private static readonly Dictionary<Type, int> StatusCodesByExceptionType = new()
    {
        [typeof(InvalidCredentialsException)] = StatusCodes.Status401Unauthorized,
        [typeof(InvalidRefreshTokenException)] = StatusCodes.Status401Unauthorized,
        [typeof(AccountSuspendedException)] = StatusCodes.Status403Forbidden,
        [typeof(TenantAccessDeniedException)] = StatusCodes.Status403Forbidden,
        [typeof(PatientNotFoundException)] = StatusCodes.Status404NotFound,
        [typeof(AlertNotFoundException)] = StatusCodes.Status404NotFound,
        [typeof(ReportNotFoundException)] = StatusCodes.Status404NotFound,
        [typeof(ClinicianNotFoundException)] = StatusCodes.Status404NotFound,
        [typeof(EmailAlreadyInUseException)] = StatusCodes.Status409Conflict,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            var isKnownException = StatusCodesByExceptionType.TryGetValue(ex.GetType(), out var statusCode);

            if (!isKnownException)
            {
                // Never leak exception details (stack trace, connection strings, etc.)
                // to the client - only the traceId, which the client can hand back to
                // support so the matching entry can be found in the server logs.
                statusCode = StatusCodes.Status500InternalServerError;
                logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);
            }

            var message = isKnownException ? ex.Message : "An unexpected error occurred. Please try again later.";

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse(statusCode, message, traceId));
        }
    }
}
