using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoutingService.Application.Abstractions;
using RoutingService.Application.Exceptions;
using RoutingService.Application.Models;
using RoutingService.Application.Options;
using RoutingService.Infrastructure.Routing;

namespace RoutingService.Tests;

public sealed class OsrmRouteProviderTests
{
    [Fact]
    public async Task GetRouteAsync_success_maps_osrm_json()
    {
        const string json = """
            {
              "code": "Ok",
              "routes": [{ "distance": 100, "duration": 20 }]
            }
            """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var provider = CreateProvider(handler, o =>
        {
            o.BaseUrl = "http://localhost:5000";
            o.AllowedProfiles = ["driving"];
        });

        var result = await provider.GetRouteAsync(
            new GeoPoint(53.9, 27.5),
            new GeoPoint(54.0, 27.6),
            "driving");

        Assert.Equal("osrm", result.Provider);
        Assert.Equal(100, result.DistanceMeters);
        Assert.Equal(20, result.DurationSeconds);
    }

    [Fact]
    public async Task GetRouteAsync_throws_on_non_success_status()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("nope", Encoding.UTF8, "text/plain")
        });

        var provider = CreateProvider(handler, o =>
        {
            o.BaseUrl = "http://localhost:5000";
            o.AllowedProfiles = ["driving"];
        });

        await Assert.ThrowsAsync<OsrmInvalidResponseException>(() =>
            provider.GetRouteAsync(new GeoPoint(0, 0), new GeoPoint(1, 1), "driving"));
    }

    [Fact]
    public async Task GetRouteAsync_times_out()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            throw new TaskCanceledException("timeout", new TimeoutException());
        });

        var provider = CreateProvider(handler, o =>
        {
            o.BaseUrl = "http://localhost:5000";
            o.TimeoutMs = 50;
            o.AllowedProfiles = ["driving"];
        });

        await Assert.ThrowsAsync<OsrmTimeoutException>(() =>
            provider.GetRouteAsync(new GeoPoint(0, 0), new GeoPoint(1, 1), "driving"));
    }

    [Fact]
    public async Task GetMatrixAsync_success_parses_table()
    {
        const string json = """
            {
              "code": "Ok",
              "durations": [[0, 5],[7, 0]],
              "distances": [[0, 50],[70, 0]]
            }
            """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var provider = CreateProvider(handler, o =>
        {
            o.BaseUrl = "http://localhost:5000";
            o.AllowedProfiles = ["driving"];
        });

        var points = new[]
        {
            new GeoPoint(0, 0),
            new GeoPoint(1, 1)
        };

        var result = await provider.GetMatrixAsync(points, "driving");

        Assert.Equal(5, result.DurationsSeconds[0][1]);
        Assert.Equal(70, result.DistancesMeters[1][0]);
    }

    [Fact]
    public async Task GetRouteAsync_profile_not_allowed_throws()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var provider = CreateProvider(handler, o =>
        {
            o.BaseUrl = "http://localhost:5000";
            o.AllowedProfiles = ["driving"];
        });

        await Assert.ThrowsAsync<ProfileNotSupportedException>(() =>
            provider.GetRouteAsync(new GeoPoint(0, 0), new GeoPoint(1, 1), "walking"));
    }

    private static OsrmRouteProvider CreateProvider(HttpMessageHandler handler, Action<OsrmOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddOptions<OsrmOptions>().Configure(configure);
        services
            .AddHttpClient(OsrmRouteProvider.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var options = sp.GetRequiredService<IOptions<OsrmOptions>>();
        return new OsrmRouteProvider(factory, options, NullLogger<OsrmRouteProvider>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
