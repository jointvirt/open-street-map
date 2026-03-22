using System.ComponentModel.DataAnnotations;

namespace RoutingService.Api.Contracts;

public sealed class RouteRequestDto
{
    /// <summary>Москва: район Театральной / центр (пример для Swagger).</summary>
    [Required]
    public CoordinateDto? Origin { get; set; } = new() { Latitude = 55.7522, Longitude = 37.6156 };

    /// <summary>Москва: севернее, ВДНХ / Останкино (пример для Swagger).</summary>
    [Required]
    public CoordinateDto? Destination { get; set; } = new() { Latitude = 55.7987, Longitude = 37.6514 };

    [RegularExpression("^(driving|walking|cycling)$", ErrorMessage = "Profile must be driving, walking, or cycling.")]
    public string? Profile { get; set; } = "driving";
}
