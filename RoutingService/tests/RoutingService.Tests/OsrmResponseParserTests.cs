using System.Text.Json;
using RoutingService.Application.Exceptions;
using RoutingService.Infrastructure.Routing;

namespace RoutingService.Tests;

public sealed class OsrmResponseParserTests
{
    [Fact]
    public void ParseRoute_success_extracts_distance_duration_and_optional_geometry()
    {
        const string json = """
            {
              "code": "Ok",
              "routes": [
                {
                  "distance": 1234.5,
                  "duration": 99.25,
                  "geometry": "abc"
                }
              ]
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var (distance, duration, geometry) = OsrmResponseParser.ParseRoute(doc);

        Assert.Equal(1234.5, distance);
        Assert.Equal(99.25, duration);
        Assert.Equal("abc", geometry);
    }

    [Fact]
    public void ParseRoute_throws_when_routes_empty()
    {
        const string json = """{"code":"Ok","routes":[]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Throws<OsrmInvalidResponseException>(() => OsrmResponseParser.ParseRoute(doc));
    }

    [Fact]
    public void ParseMatrix_success_parses_nullable_cells()
    {
        const string json = """
            {
              "code": "Ok",
              "durations": [[0, 10, null],[5, 0, 15]],
              "distances": [[0, 100, null],[50, 0, 200]]
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var (durations, distances) = OsrmResponseParser.ParseMatrix(doc);

        Assert.Null(durations[0][2]);
        Assert.Equal(10, durations[0][1]);
        Assert.Null(distances[0][2]);
        Assert.Equal(200, distances[1][2]);
    }
}
