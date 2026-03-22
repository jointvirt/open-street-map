namespace RoutingService.Api.Contracts;

public sealed class MatrixResponseDto
{
    public required double?[][] DurationsSeconds { get; init; }
    public required double?[][] DistancesMeters { get; init; }
    public required string Provider { get; init; }
}
