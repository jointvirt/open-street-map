namespace RoutingService.Application.Exceptions;

public class OsrmRoutingException : RoutingServiceException
{
    public OsrmRoutingException(string message) : base(message)
    {
    }

    public OsrmRoutingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
