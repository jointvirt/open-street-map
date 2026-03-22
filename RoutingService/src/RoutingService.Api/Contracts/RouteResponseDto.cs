namespace RoutingService.Api.Contracts;

public sealed class RouteResponseDto
{
    public required double DistanceMeters { get; init; }
    public required double DurationSeconds { get; init; }
    public required double DurationMinutes { get; init; }
    public string? Geometry { get; init; }
    public required string Provider { get; init; }
}
