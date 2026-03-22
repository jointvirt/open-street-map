using System.Text.Json;
using RoutingService.Application.Exceptions;

namespace RoutingService.Infrastructure.Routing;

internal static class OsrmResponseParser
{
    public static (double DistanceMeters, double DurationSeconds, string? Geometry) ParseRoute(JsonDocument document)
    {
        var root = document.RootElement;
        if (!TryGetString(root, "code", out var code) || !string.Equals(code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            var msg = TryGetString(root, "message", out var m) ? m : "Unexpected OSRM response code.";
            throw new OsrmInvalidResponseException($"OSRM route response was not OK: {code}. {msg}");
        }

        if (!root.TryGetProperty("routes", out var routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
            throw new OsrmInvalidResponseException("OSRM route response contained no routes.");

        var route = routes[0];
        if (!route.TryGetProperty("distance", out var distEl) || distEl.ValueKind != JsonValueKind.Number)
            throw new OsrmInvalidResponseException("OSRM route response contained no distance in meters.");

        if (!route.TryGetProperty("duration", out var durEl) || durEl.ValueKind != JsonValueKind.Number)
            throw new OsrmInvalidResponseException("OSRM route response contained no duration in seconds.");

        var distance = distEl.GetDouble();
        var duration = durEl.GetDouble();

        string? geometry = null;
        if (route.TryGetProperty("geometry", out var geomEl) && geomEl.ValueKind == JsonValueKind.String)
            geometry = geomEl.GetString();

        return (distance, duration, geometry);
    }

    public static (double?[][] Durations, double?[][] Distances) ParseMatrix(JsonDocument document)
    {
        var root = document.RootElement;
        if (!TryGetString(root, "code", out var code) || !string.Equals(code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            var msg = TryGetString(root, "message", out var m) ? m : "Unexpected OSRM response code.";
            throw new OsrmInvalidResponseException($"OSRM table response was not OK: {code}. {msg}");
        }

        if (!root.TryGetProperty("durations", out var durationsEl) || durationsEl.ValueKind != JsonValueKind.Array)
            throw new OsrmInvalidResponseException("OSRM table response contained no durations matrix.");

        if (!root.TryGetProperty("distances", out var distancesEl) || distancesEl.ValueKind != JsonValueKind.Array)
            throw new OsrmInvalidResponseException("OSRM table response contained no distances matrix.");

        var durations = ParseNullableMatrix(durationsEl);
        var distances = ParseNullableMatrix(distancesEl);

        if (durations.Length != distances.Length)
            throw new OsrmInvalidResponseException("OSRM table response contained mismatched matrix dimensions.");

        for (var i = 0; i < durations.Length; i++)
        {
            if (durations[i].Length != distances[i].Length)
                throw new OsrmInvalidResponseException("OSRM table response contained mismatched matrix row dimensions.");
        }

        return (durations, distances);
    }

    private static double?[][] ParseNullableMatrix(JsonElement matrix)
    {
        var rows = new List<double?[]>();
        foreach (var row in matrix.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
                throw new OsrmInvalidResponseException("OSRM table matrix row was not an array.");

            var cells = new List<double?>();
            foreach (var cell in row.EnumerateArray())
            {
                if (cell.ValueKind == JsonValueKind.Null)
                {
                    cells.Add(null);
                    continue;
                }

                if (cell.ValueKind != JsonValueKind.Number)
                    throw new OsrmInvalidResponseException("OSRM table matrix cell was not a number or null.");

                cells.Add(cell.GetDouble());
            }

            rows.Add(cells.ToArray());
        }

        return rows.ToArray();
    }

    private static bool TryGetString(JsonElement element, string name, out string value)
    {
        value = "";
        if (!element.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
            return false;

        value = prop.GetString() ?? "";
        return true;
    }
}
