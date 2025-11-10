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
    /// Get all top-level areas (no parent) with climb counts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AreaSummaryDto>>> GetTopLevel()
    {
        var areas = await _db.Areas
            .Where(a => a.ParentAreaId == null)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<AreaSummaryDto>();
        
        foreach (var area in areas)
        {
            // Count all climbs in this area and all sub-areas recursively
            var climbCount = await GetTotalClimbCount(area.Id);
            
            result.Add(new AreaSummaryDto
            {
                Id = area.Id,
                Name = area.Name,
                ClimbCount = climbCount
            });
        }

        return result;
    }

    /// <summary>
    /// Get area with its sub-areas and climbs
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

        var subAreaDtos = new List<AreaSummaryDto>();
        foreach (var subArea in area.SubAreas)
        {
            var climbCount = await GetTotalClimbCount(subArea.Id);
            subAreaDtos.Add(new AreaSummaryDto
            {
                Id = subArea.Id,
                Name = subArea.Name,
                ClimbCount = climbCount
            });
        }

        return Ok(new AreaDetailDto
        {
            Id = area.Id,
            Name = area.Name,
            State = area.State,
            Lat = area.Lat,
            Lng = area.Lng,
            ParentAreaId = area.ParentAreaId,
            SubAreas = subAreaDtos,
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
    /// Recursively count all climbs in an area and its sub-areas
    /// </summary>
    private async Task<int> GetTotalClimbCount(int areaId)
    {
        // Count climbs directly on this area
        var directClimbCount = await _db.Climbs.CountAsync(c => c.AreaId == areaId);

        // Get all sub-areas
        var subAreaIds = await _db.Areas
            .Where(a => a.ParentAreaId == areaId)
            .Select(a => a.Id)
            .ToListAsync();

        // Recursively count climbs in sub-areas
        var subAreaClimbCount = 0;
        foreach (var subAreaId in subAreaIds)
        {
            subAreaClimbCount += await GetTotalClimbCount(subAreaId);
        }

        return directClimbCount + subAreaClimbCount;
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