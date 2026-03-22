using System.ComponentModel.DataAnnotations;

namespace RoutingService.Api.Contracts;

public sealed class MatrixRequestDto : IValidatableObject
{
    [Required]
    [MinLength(2, ErrorMessage = "At least 2 points are required.")]
    [MaxLength(100, ErrorMessage = "A maximum of 100 points is supported.")]
    public List<CoordinateDto>? Points { get; set; } =
    [
        new() { Latitude = 55.7522, Longitude = 37.6156 },
        new() { Latitude = 55.7987, Longitude = 37.6514 },
        new() { Latitude = 55.7188, Longitude = 37.6076 }
    ];

    [RegularExpression("^(driving|walking|cycling)$", ErrorMessage = "Profile must be driving, walking, or cycling.")]
    public string? Profile { get; set; } = "driving";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Points is null)
            yield break;

        for (var i = 0; i < Points.Count; i++)
        {
            var ctx = new ValidationContext(Points[i], validationContext, null);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(Points[i], ctx, results, validateAllProperties: true))
            {
                foreach (var r in results)
                    yield return new ValidationResult(r.ErrorMessage, new[] { $"{nameof(Points)}[{i}]" });
            }
        }
    }
}
