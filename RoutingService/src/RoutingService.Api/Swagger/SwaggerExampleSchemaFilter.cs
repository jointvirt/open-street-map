using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RoutingService.Api.Contracts;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoutingService.Api.Swagger;

/// <summary>
/// Примеры координат Москвы для подстановки в Swagger UI (Example Value).
/// </summary>
public sealed class SwaggerExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RouteRequestDto))
        {
            schema.Example = new OpenApiObject
            {
                ["origin"] = Coord(55.7522, 37.6156),
                ["destination"] = Coord(55.7987, 37.6514),
                ["profile"] = new OpenApiString("driving")
            };
            return;
        }

        if (context.Type == typeof(MatrixRequestDto))
        {
            schema.Example = new OpenApiObject
            {
                ["points"] = new OpenApiArray
                {
                    Coord(55.7522, 37.6156),
                    Coord(55.7987, 37.6514),
                    Coord(55.7188, 37.6076)
                },
                ["profile"] = new OpenApiString("driving")
            };
        }
    }

    private static OpenApiObject Coord(double lat, double lon) =>
        new()
        {
            ["latitude"] = new OpenApiDouble(lat),
            ["longitude"] = new OpenApiDouble(lon)
        };
}
