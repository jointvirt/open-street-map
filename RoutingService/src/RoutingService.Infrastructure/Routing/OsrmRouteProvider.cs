using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoutingService.Application;
using RoutingService.Application.Abstractions;
using RoutingService.Application.Exceptions;
using RoutingService.Application.Models;
using RoutingService.Application.Options;

namespace RoutingService.Infrastructure.Routing;

public sealed class OsrmRouteProvider : IRouteProvider
{
    public const string HttpClientName = "osrm";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<OsrmOptions> _options;
    private readonly ILogger<OsrmRouteProvider> _logger;

    public OsrmRouteProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OsrmOptions> options,
        ILogger<OsrmRouteProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<RouteResult> GetRouteAsync(
        GeoPoint origin,
        GeoPoint destination,
        string profile,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        EnsureProfileAllowed(profile);

        var osrmProfile = RoutingProfiles.ToOsrmEngineProfile(profile);
        var opt = _options.Value;

        var uri = BuildRouteUri(
            opt.BaseUrl,
            osrmProfile,
            origin.Longitude,
            origin.Latitude,
            destination.Longitude,
            destination.Latitude,
            opt.EnableGeometry);

        _logger.LogInformation(
            "Route request started: {EndpointKind} profile={OsrmProfile} elapsedMs={ElapsedMs}",
            "route",
            osrmProfile,
            sw.ElapsedMilliseconds);

        _logger.LogDebug("OSRM route template: {BaseUrl}/route/v1/{Profile}/{{coordinates}}", SanitizeBaseUrl(opt.BaseUrl), osrmProfile);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await SendOsrmAsync(client, request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Route request timed out after {ElapsedMs} ms", sw.ElapsedMilliseconds);
            throw new OsrmTimeoutException();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await ReadBodyPreviewAsync(responseStream, cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "OSRM route returned HTTP {StatusCode}. Body preview: {BodyPreview}",
                (int)response.StatusCode,
                body);
            throw new OsrmInvalidResponseException($"OSRM route response failed with HTTP {(int)response.StatusCode}.");
        }

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OSRM route JSON.");
            throw new OsrmInvalidResponseException("OSRM route response contained invalid JSON.", ex);
        }

        using (document)
        {
            try
            {
                var (distanceMeters, durationSeconds, geometry) = OsrmResponseParser.ParseRoute(document);
                sw.Stop();
                _logger.LogInformation(
                    "Route request completed: profile={OsrmProfile} distanceM={DistanceMeters:F1} durationS={DurationSeconds:F1} elapsedMs={ElapsedMs}",
                    osrmProfile,
                    distanceMeters,
                    durationSeconds,
                    sw.ElapsedMilliseconds);

                return new RouteResult
                {
                    DistanceMeters = distanceMeters,
                    DurationSeconds = durationSeconds,
                    Geometry = opt.EnableGeometry ? geometry : null,
                    Provider = "osrm"
                };
            }
            catch (OsrmInvalidResponseException)
            {
                sw.Stop();
                _logger.LogError("Route request failed after {ElapsedMs} ms", sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    public async Task<MatrixResult> GetMatrixAsync(
        IReadOnlyList<GeoPoint> points,
        string profile,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        EnsureProfileAllowed(profile);

        var osrmProfile = RoutingProfiles.ToOsrmEngineProfile(profile);
        var opt = _options.Value;

        var uri = BuildTableUri(opt.BaseUrl, osrmProfile, points);

        _logger.LogInformation(
            "Matrix request started: {EndpointKind} profile={OsrmProfile} points={PointCount} elapsedMs={ElapsedMs}",
            "table",
            osrmProfile,
            points.Count,
            sw.ElapsedMilliseconds);

        _logger.LogDebug("OSRM table template: {BaseUrl}/table/v1/{Profile}/{{coordinates}}", SanitizeBaseUrl(opt.BaseUrl), osrmProfile);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await SendOsrmAsync(client, request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Matrix request timed out after {ElapsedMs} ms", sw.ElapsedMilliseconds);
            throw new OsrmTimeoutException();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await ReadBodyPreviewAsync(responseStream, cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "OSRM table returned HTTP {StatusCode}. Body preview: {BodyPreview}",
                (int)response.StatusCode,
                body);
            throw new OsrmInvalidResponseException($"OSRM table response failed with HTTP {(int)response.StatusCode}.");
        }

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OSRM table JSON.");
            throw new OsrmInvalidResponseException("OSRM table response contained invalid JSON.", ex);
        }

        using (document)
        {
            var (durations, distances) = OsrmResponseParser.ParseMatrix(document);
            sw.Stop();
            _logger.LogInformation(
                "Matrix request completed: profile={OsrmProfile} size={Size} elapsedMs={ElapsedMs}",
                osrmProfile,
                points.Count,
                sw.ElapsedMilliseconds);

            return new MatrixResult
            {
                DurationsSeconds = durations,
                DistancesMeters = distances,
                Provider = "osrm"
            };
        }
    }

    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        var opt = _options.Value;
        var osrmProfile = RoutingProfiles.ToOsrmEngineProfile(RoutingProfiles.Driving);
        // Короткий отрезок в Москве — должен строиться на российском (и типичном региональном) extract.
        var uri = BuildRouteUri(
            opt.BaseUrl,
            osrmProfile,
            37.6173,
            55.7558,
            37.6273,
            55.7658,
            enableGeometry: false);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await SendOsrmAsync(client, request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OsrmTimeoutException();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new OsrmInvalidResponseException($"OSRM readiness probe failed with HTTP {(int)response.StatusCode}.");

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new OsrmInvalidResponseException("OSRM readiness probe returned invalid JSON.", ex);
        }

        using (document)
        {
            _ = OsrmResponseParser.ParseRoute(document);
        }
    }

    private async Task<HttpResponseMessage> SendOsrmAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "OSRM HTTP request failed (host unreachable or DNS). BaseUrl={BaseUrl}",
                SanitizeBaseUrl(_options.Value.BaseUrl));
            throw new OsrmUnreachableException(
                "Could not reach the OSRM routing engine at the configured BaseUrl. " +
                "In Docker, ensure OSRM is running and port 5000 is published; the default is http://host.docker.internal:5000 (see docker-compose). " +
                "For `dotnet run` on the host, set Routing__Osrm__BaseUrl=http://localhost:5000. " +
                "If your environment resolves Docker service DNS, you can try Routing__Osrm__BaseUrl=http://osrm:5000.",
                ex);
        }
    }

    private void EnsureProfileAllowed(string profile)
    {
        if (!_options.Value.AllowedProfiles.Contains(profile, StringComparer.OrdinalIgnoreCase))
            throw new ProfileNotSupportedException(profile);
    }

    internal static Uri BuildRouteUri(
        string baseUrl,
        string osrmProfile,
        double originLon,
        double originLat,
        double destLon,
        double destLat,
        bool enableGeometry)
    {
        var overview = enableGeometry ? "simplified" : "false";
        var coords = $"{originLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{originLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{destLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{destLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var path = $"/route/v1/{Uri.EscapeDataString(osrmProfile)}/{coords}";
        var query = $"overview={overview}&steps=false&annotations=false";
        return Combine(baseUrl, path, query);
    }

    internal static Uri BuildTableUri(string baseUrl, string osrmProfile, IReadOnlyList<GeoPoint> points)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < points.Count; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(points[i].Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(points[i].Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var path = $"/table/v1/{Uri.EscapeDataString(osrmProfile)}/{sb}";
        var query = "annotations=duration,distance";
        return Combine(baseUrl, path, query);
    }

    private static Uri Combine(string baseUrl, string pathAndQuery, string query)
    {
        var root = baseUrl.TrimEnd('/');
        var uri = new Uri(new Uri(root + "/", UriKind.Absolute), pathAndQuery.TrimStart('/') + "?" + query);
        return uri;
    }

    private static string SanitizeBaseUrl(string baseUrl) => baseUrl.TrimEnd('/');

    private static async Task<string> ReadBodyPreviewAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return text.Length <= 512 ? text : text[..512] + "…";
        }
        catch
        {
            return "<unreadable>";
        }
    }
}
