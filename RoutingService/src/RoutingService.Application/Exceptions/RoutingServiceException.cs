namespace RoutingService.Application.Exceptions;

public abstract class RoutingServiceException : Exception
{
    protected RoutingServiceException(string message) : base(message)
    {
    }

    protected RoutingServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
