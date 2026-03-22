using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RoutingService.Application.Exceptions;

namespace RoutingService.Api.Exceptions;

public sealed class RoutingExceptionHandler : IExceptionHandler
{
    private readonly ILogger<RoutingExceptionHandler> _logger;

    public RoutingExceptionHandler(ILogger<RoutingExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RoutingServiceException routingException)
            return false;

        _logger.LogError(exception, "Routing request failed.");

        var (status, title) = routingException switch
        {
            ProfileNotSupportedException => (StatusCodes.Status400BadRequest, "Unsupported profile"),
            OsrmTimeoutException => (StatusCodes.Status504GatewayTimeout, "Routing engine timeout"),
            OsrmUnreachableException => (StatusCodes.Status502BadGateway, "Routing engine unreachable"),
            OsrmInvalidResponseException => (StatusCodes.Status502BadGateway, "Routing engine returned an invalid response"),
            OsrmRoutingException => (StatusCodes.Status502BadGateway, "Routing engine error"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected routing error")
        };

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = routingException.Message,
            Instance = httpContext.Request.Path.Value
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
