using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tufo.Core.Entities;
using Tufo.Infrastructure;

namespace Tufo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AreasController : ControllerBase
{
    private readonly TufoContext _db;
    public AreasController(TufoContext db) => _db = db;

    /// <summary>
    /// Get all top-level areas (no parent)
    /// </summary>
    [HttpGet]
    public Task<List<Area>> GetTopLevel() =>
        _db.Areas
            .Where(a => a.ParentAreaId == null)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>
    /// Get area with its sub-areas
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AreaDetailDto>> GetOne(int id)
    {
        var area = await _db.Areas
            .Include(a => a.SubAreas)
            .Include(a => a.Climbs)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (area == null)
            return NotFound();

        return Ok(new AreaDetailDto
        {
            Id = area.Id,
            Name = area.Name,
            State = area.State,
            Lat = area.Lat,
            Lng = area.Lng,
            ParentAreaId = area.ParentAreaId,
            SubAreas = area.SubAreas.Select(s => new AreaSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                ClimbCount = _db.Climbs.Count(c => c.AreaId == s.Id)
            }).ToList(),
            Climbs = area.Climbs.Select(c => new ClimbSummaryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Yds = c.Yds
            }).ToList()
        });
    }

    /// <summary>
    /// Search areas by name
    /// </summary>
    [HttpGet("search")]
    public async Task<List<Area>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return new List<Area>();

        return await _db.Areas
            .Where(a => EF.Functions.ILike(a.Name, $"%{q}%"))
            .Take(20)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Area>> Create(Area area)
    {
        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOne), new { id = area.Id }, area);
    }
}

// DTOs
public class AreaDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? State { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public int? ParentAreaId { get; set; }
    public List<AreaSummaryDto> SubAreas { get; set; } = new();
    public List<ClimbSummaryDto> Climbs { get; set; } = new();
}

public class AreaSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int ClimbCount { get; set; }
}

public class ClimbSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Yds { get; set; }
}