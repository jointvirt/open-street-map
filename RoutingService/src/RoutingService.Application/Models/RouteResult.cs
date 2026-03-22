namespace RoutingService.Application.Models;

public sealed class RouteResult
{
    public required double DistanceMeters { get; init; }
    public required double DurationSeconds { get; init; }
    public string? Geometry { get; init; }
    public required string Provider { get; init; }
}
