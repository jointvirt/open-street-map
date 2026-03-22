using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RoutingService.Api.Contracts;
using RoutingService.Application.Abstractions;
using RoutingService.Application.Models;
using RoutingService.Application.Options;

namespace RoutingService.Api.Controllers;

[ApiController]
[Route("api/routes")]
public sealed class RoutesController : ControllerBase
{
    private readonly IRouteProvider _routeProvider;
    private readonly OsrmOptions _options;

    public RoutesController(IRouteProvider routeProvider, IOptions<OsrmOptions> options)
    {
        _routeProvider = routeProvider;
        _options = options.Value;
    }

    /// <summary>Compute a route between two coordinates using OSRM.</summary>
    [HttpPost("route")]
    [ProducesResponseType(typeof(RouteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<RouteResponseDto>> Route([FromBody] RouteRequestDto request, CancellationToken cancellationToken)
    {
        var profile = string.IsNullOrWhiteSpace(request.Profile) ? _options.DefaultProfile : request.Profile!;
        var origin = new GeoPoint(request.Origin!.Latitude, request.Origin.Longitude);
        var destination = new GeoPoint(request.Destination!.Latitude, request.Destination.Longitude);

        var result = await _routeProvider.GetRouteAsync(origin, destination, profile, cancellationToken).ConfigureAwait(false);
        return Ok(ToDto(result));
    }

    /// <summary>Compute duration/distance matrices between many coordinates using OSRM table service.</summary>
    [HttpPost("matrix")]
    [ProducesResponseType(typeof(MatrixResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<MatrixResponseDto>> Matrix([FromBody] MatrixRequestDto request, CancellationToken cancellationToken)
    {
        var profile = string.IsNullOrWhiteSpace(request.Profile) ? _options.DefaultProfile : request.Profile!;
        var points = request.Points!.Select(p => new GeoPoint(p.Latitude, p.Longitude)).ToList();

        var result = await _routeProvider.GetMatrixAsync(points, profile, cancellationToken).ConfigureAwait(false);
        return Ok(ToDto(result));
    }

    private static RouteResponseDto ToDto(RouteResult result) =>
        new()
        {
            DistanceMeters = result.DistanceMeters,
            DurationSeconds = result.DurationSeconds,
            DurationMinutes = result.DurationSeconds / 60.0,
            Geometry = result.Geometry,
            Provider = result.Provider
        };

    private static MatrixResponseDto ToDto(MatrixResult result) =>
        new()
        {
            DurationsSeconds = result.DurationsSeconds,
            DistancesMeters = result.DistancesMeters,
            Provider = result.Provider
        };
}
