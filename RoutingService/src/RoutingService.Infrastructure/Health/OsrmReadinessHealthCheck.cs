using Microsoft.Extensions.Diagnostics.HealthChecks;
using RoutingService.Application.Abstractions;

namespace RoutingService.Infrastructure.Health;

public sealed class OsrmReadinessHealthCheck : IHealthCheck
{
    private readonly IRouteProvider _routeProvider;

    public OsrmReadinessHealthCheck(IRouteProvider routeProvider)
    {
        _routeProvider = routeProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _routeProvider.PingAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OSRM is not reachable or returned an invalid response.", ex);
        }
    }
}
