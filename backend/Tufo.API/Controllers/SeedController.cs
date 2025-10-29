using Microsoft.AspNetCore.Mvc;
using Tufo.Core.Entities;
using Tufo.Infrastructure;

namespace Tufo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly TufoContext _db;
    public SeedController(TufoContext db) => _db = db;

    [HttpPost("sample")]
    public async Task<IActionResult> Sample()
    {
        if (_db.Areas.Any()) return Ok("Already seeded.");

        var area = new Area { Name = "Lake Mineral Wells State Park", State = "TX", Lat = 32.8134, Lng = -98.0588 };
        _db.Areas.Add(area);

        _db.Climbs.AddRange(
            new Climb {
                Area = area, Name = "Black Sabbath", Type = "Sport", Yds = "5.10a",
                Description = "Face climbing on edges; good intro to the style.",
                HeroUrl = "/uploads/sample_black_sabbath_overhead.jpg",
                HeroAttribution = "Photo © Tufo Sample",
                Source = "Sample"
            },
            new Climb {
                Area = area, Name = "Bird Dog", Type = "Sport", Yds = "5.8",
                Description = "Friendly warm-up with a mellow crux midway.",
                HeroUrl = "/uploads/sample_birddog_overhead.jpg",
                HeroAttribution = "Photo © Tufo Sample",
                Source = "Sample"
            }
        );

        await _db.SaveChangesAsync();
        return Ok("Seeded.");
    }
}