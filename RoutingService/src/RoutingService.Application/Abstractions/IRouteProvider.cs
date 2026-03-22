using RoutingService.Application.Models;

namespace RoutingService.Application.Abstractions;

public interface IRouteProvider
{
    Task<RouteResult> GetRouteAsync(
        GeoPoint origin,
        GeoPoint destination,
        string profile,
        CancellationToken cancellationToken = default);

    Task<MatrixResult> GetMatrixAsync(
        IReadOnlyList<GeoPoint> points,
        string profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the routing engine responds successfully (used for readiness probes).
    /// </summary>
    Task PingAsync(CancellationToken cancellationToken = default);
}
