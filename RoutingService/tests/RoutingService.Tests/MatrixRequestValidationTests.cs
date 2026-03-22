using System.ComponentModel.DataAnnotations;
using RoutingService.Api.Contracts;

namespace RoutingService.Tests;

public sealed class MatrixRequestValidationTests
{
    [Fact]
    public void Matrix_too_few_points_fails_validation()
    {
        var dto = new MatrixRequestDto
        {
            Points = [new CoordinateDto { Latitude = 0, Longitude = 0 }]
        };

        var results = Validate(dto);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MatrixRequestDto.Points)));
    }

    [Fact]
    public void Matrix_too_many_points_fails_validation()
    {
        var dto = new MatrixRequestDto
        {
            Points = Enumerable.Range(0, 101).Select(_ => new CoordinateDto { Latitude = 0, Longitude = 0 }).ToList()
        };

        var results = Validate(dto);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MatrixRequestDto.Points)));
    }

    [Fact]
    public void Matrix_invalid_coordinate_fails_validation()
    {
        var dto = new MatrixRequestDto
        {
            Points =
            [
                new CoordinateDto { Latitude = 0, Longitude = 0 },
                new CoordinateDto { Latitude = 200, Longitude = 0 }
            ]
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    private static List<ValidationResult> Validate(MatrixRequestDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        foreach (var r in dto.Validate(context))
            results.Add(r);

        return results;
    }
}
