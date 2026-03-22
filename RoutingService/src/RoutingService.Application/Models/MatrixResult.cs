namespace RoutingService.Application.Models;

public sealed class MatrixResult
{
    public required double?[][] DurationsSeconds { get; init; }
    public required double?[][] DistancesMeters { get; init; }
    public required string Provider { get; init; }
}
