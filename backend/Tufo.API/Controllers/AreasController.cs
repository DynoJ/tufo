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
    /// Get all states with their area and climb counts
    /// </summary>
    [HttpGet("by-state")]
    public async Task<ActionResult<List<StateSummaryDto>>> GetByState()
    {
        var areas = await _db.Areas
            .Where(a => a.ParentAreaId == null && a.State != null)
            .AsNoTracking()
            .ToListAsync();

        var stateGroups = areas
            .GroupBy(a => a.State)
            .Select(g => new StateSummaryDto
            {
                State = g.Key!,
                AreaCount = g.Count(),
                ClimbCount = 0 // Will calculate below
            })
            .OrderBy(s => s.State)
            .ToList();

        // Calculate climb counts for each state
        foreach (var state in stateGroups)
        {
            var stateAreas = areas.Where(a => a.State == state.State).Select(a => a.Id).ToList();
            var totalClimbs = 0;
            
            foreach (var areaId in stateAreas)
            {
                totalClimbs += await GetTotalClimbCount(areaId);
            }
            
            state.ClimbCount = totalClimbs;
        }

        return stateGroups;
    }

    /// <summary>
    /// Get top-level areas for a specific state
    /// </summary>
    [HttpGet("by-state/{state}")]
    public async Task<ActionResult<List<AreaSummaryDto>>> GetAreasInState(string state)
    {
        var areas = await _db.Areas
            .Where(a => a.ParentAreaId == null && a.State == state)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<AreaSummaryDto>();
        
        foreach (var area in areas)
        {
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
    /// Get climbing areas near a location (parent areas only - like "Barton Creek Greenbelt", "Gus Fruh")
    /// </summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyAreas(
        [FromQuery] double lat, 
        [FromQuery] double lng, 
        [FromQuery] int radius = 50)
    {
        // Get all areas with coordinates and include their sub-areas to determine if they're parent areas
        var areas = await _db.Areas
            .Where(a => a.Lat.HasValue && a.Lng.HasValue)
            .Include(a => a.SubAreas)
            .AsNoTracking()
            .ToListAsync();

        // Filter to only parent areas (those with sub-areas OR top-level areas with no parent)
        var parentAreas = areas.Where(a => a.SubAreas.Any() || a.ParentAreaId == null).ToList();

        // Calculate distances and filter by radius
        var nearbyAreas = new List<(Area area, double distance)>();
        
        foreach (var area in parentAreas)
        {
            var distance = CalculateDistance(lat, lng, area.Lat!.Value, area.Lng!.Value);
            if (distance <= radius)
            {
                nearbyAreas.Add((area, distance));
            }
        }

        // Sort by distance and get climb counts
        var result = new List<AreaSummaryDto>();
        foreach (var (area, _) in nearbyAreas.OrderBy(x => x.distance).Take(20))
        {
            var climbCount = await GetTotalClimbCount(area.Id);
            result.Add(new AreaSummaryDto
            {
                Id = area.Id,
                Name = area.Name,
                ClimbCount = climbCount,
                Lat = area.Lat,
                Lng = area.Lng
            });
        }

        return Ok(result);
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

    /// <summary>
    /// Calculate distance between two coordinates in miles using Haversine formula
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3959; // Earth's radius in miles
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}

// DTOs
public class StateSummaryDto
{
    public string State { get; set; } = null!;
    public int AreaCount { get; set; }
    public int ClimbCount { get; set; }
}

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
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

public class ClimbSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Yds { get; set; }
}