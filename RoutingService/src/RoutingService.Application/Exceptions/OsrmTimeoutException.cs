namespace RoutingService.Application.Exceptions;

public sealed class OsrmTimeoutException : OsrmRoutingException
{
    public OsrmTimeoutException()
        : base("The routing engine did not respond in time.")
    {
    }
}
