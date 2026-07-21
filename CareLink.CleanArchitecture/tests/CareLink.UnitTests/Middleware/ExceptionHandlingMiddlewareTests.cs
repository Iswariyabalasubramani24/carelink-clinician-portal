using CareLink.API.Middleware;
using CareLink.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CareLink.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, string Body)> InvokeAsync(RequestDelegate next, ILogger<ExceptionHandlingMiddleware>? logger = null)
    {
        var middleware = new ExceptionHandlingMiddleware(next, logger ?? new Mock<ILogger<ExceptionHandlingMiddleware>>().Object);

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ReturnsGenericFiveHundredWithoutLeakingExceptionDetails()
    {
        // The exception message here deliberately contains something that would be
        // sensitive if ever echoed back to a client (e.g. connection details) - the
        // point of the catch-all handler is that this text must never reach the response.
        RequestDelegate next = _ => throw new InvalidOperationException("Server=prod-db;Password=hunter2");

        var (statusCode, body) = await InvokeAsync(next);

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.DoesNotContain("hunter2", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("Server=prod-db", body);
        Assert.Contains("unexpected error", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_LogsFullExceptionServerSide()
    {
        var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");

        await InvokeAsync(next, loggerMock.Object);

        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.Is<Exception>(ex => ex.Message == "boom"),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(typeof(PatientNotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(TenantAccessDeniedException), StatusCodes.Status403Forbidden)]
    [InlineData(typeof(InvalidCredentialsException), StatusCodes.Status401Unauthorized)]
    [InlineData(typeof(EmailAlreadyInUseException), StatusCodes.Status409Conflict)]
    public async Task InvokeAsync_KnownApplicationException_MapsToItsDedicatedStatusCodeAndOwnMessage(Type exceptionType, int expectedStatusCode)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        RequestDelegate next = _ => throw exception;

        var (statusCode, body) = await InvokeAsync(next);

        Assert.Equal(expectedStatusCode, statusCode);
        Assert.Contains(exception.Message, body);
    }

    [Fact]
    public async Task InvokeAsync_AnyException_ResponseBodyIncludesTraceId()
    {
        RequestDelegate next = _ => throw new PatientNotFoundException();

        var (_, body) = await InvokeAsync(next);

        Assert.Contains("traceId", body, StringComparison.OrdinalIgnoreCase);
    }
}
