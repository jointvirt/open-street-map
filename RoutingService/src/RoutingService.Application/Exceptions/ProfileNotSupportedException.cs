namespace RoutingService.Application.Exceptions;

public sealed class ProfileNotSupportedException : RoutingServiceException
{
    public ProfileNotSupportedException(string profile)
        : base(
            $"The routing profile '{profile}' is not enabled for this deployment. " +
            "The default OSRM dataset is built for driving (car). " +
            "Adjust Routing:Osrm:AllowedProfiles and prepare matching OSRM profiles if needed.")
    {
        Profile = profile;
    }

    public string Profile { get; }
}
