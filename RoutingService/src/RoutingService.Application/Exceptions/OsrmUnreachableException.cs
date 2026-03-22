namespace RoutingService.Application.Exceptions;

/// <summary>
/// OSRM недоступен по сети (DNS, отказ соединения, и т.д.).
/// </summary>
public sealed class OsrmUnreachableException : OsrmRoutingException
{
    public OsrmUnreachableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
