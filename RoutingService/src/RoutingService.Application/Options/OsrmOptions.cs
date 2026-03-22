using RoutingService.Application;

namespace RoutingService.Application.Options;

public sealed class OsrmOptions
{
    public const string SectionName = "Routing:Osrm";

    /// <summary>
    /// Base URL of the OSRM HTTP service (no trailing slash), e.g. http://localhost:5000
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    public int TimeoutMs { get; set; } = 30_000;

    public string DefaultProfile { get; set; } = RoutingProfiles.Driving;

    /// <summary>
    /// When true, successful route responses may include encoded polyline geometry (larger payloads).
    /// </summary>
    public bool EnableGeometry { get; set; }

    /// <summary>
    /// API profiles allowed by this deployment (subset of driving/walking/cycling).
    /// Default Docker image ships a single car graph; only driving is enabled unless you change OSRM data.
    /// </summary>
    public string[] AllowedProfiles { get; set; } = [RoutingProfiles.Driving];
}
