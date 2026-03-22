namespace RoutingService.Application.Exceptions;

public sealed class OsrmInvalidResponseException : OsrmRoutingException
{
    public OsrmInvalidResponseException(string message) : base(message)
    {
    }

    public OsrmInvalidResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
