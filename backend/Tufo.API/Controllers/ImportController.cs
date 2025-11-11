using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tufo.API.Services;
using Tufo.Infrastructure;
using Tufo.API.Models;

namespace Tufo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly OpenBetaImporter _importer;
    private readonly TufoContext _db;

    public ImportController(OpenBetaImporter importer, TufoContext db)
    {
        _importer = importer;
        _db = db;
    }

    /// <summary>
    /// Import ALL 50 US states (sport + boulder only)
    /// </summary>
    [HttpPost("all-states")]
    public async Task<ActionResult> ImportAllStates()
    {
        var result = await _importer.ImportAllUSStates();

        if (!result.Success)
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                errors = result.Errors
            });

        return Ok(new
        {
            success = true,
            message = result.Message,
            areasImported = result.AreasImported,
            climbsImported = result.ClimbsImported,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Import a specific climbing area by name (e.g., "Barton Creek Greenbelt")
    /// This will import the location with its hierarchical structure:
    /// Location -> Sub-Areas -> Walls -> Climbs
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult> ImportBySearch([FromBody] SearchImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AreaName))
            return BadRequest("Area name is required");

        var result = await _importer.ImportAreaByName(request.AreaName);

        if (!result.Success)
            return BadRequest(new
            {
                success = false,
                errors = result.Errors
            });

        return Ok(new
        {
            success = true,
            message = $"Imported {result.ClimbsImported} climbs across {result.AreasImported} areas. Skipped {result.ClimbsSkipped} duplicates.",
            areasImported = result.AreasImported,
            climbsImported = result.ClimbsImported,
            climbsSkipped = result.ClimbsSkipped,
            errors = result.Errors
        });
    }

 /// <summary>
/// Fix duplicate state names (TX -> Texas, etc.)
/// </summary>
[HttpPost("fix-states")]
public async Task<ActionResult> FixStateNames()
{
    var stateMapping = new Dictionary<string, string>
    {
        ["TX"] = "Texas",
        ["CA"] = "California",
        ["NY"] = "New York",
        ["FL"] = "Florida",
        ["CO"] = "Colorado",
        ["WA"] = "Washington",
        ["OR"] = "Oregon",
        ["UT"] = "Utah",
        ["AZ"] = "Arizona",
        ["NV"] = "Nevada",
        ["NM"] = "New Mexico",
        ["WY"] = "Wyoming",
        ["MT"] = "Montana",
        ["ID"] = "Idaho",
        ["NC"] = "North Carolina",
        ["SC"] = "South Carolina",
        ["GA"] = "Georgia",
        ["AL"] = "Alabama",
        ["TN"] = "Tennessee",
        ["KY"] = "Kentucky",
        ["VA"] = "Virginia",
        ["WV"] = "West Virginia",
        ["OH"] = "Ohio",
        ["IN"] = "Indiana",
        ["IL"] = "Illinois",
        ["MI"] = "Michigan",
        ["WI"] = "Wisconsin",
        ["MN"] = "Minnesota",
        ["IA"] = "Iowa",
        ["MO"] = "Missouri",
        ["AR"] = "Arkansas",
        ["LA"] = "Louisiana",
        ["MS"] = "Mississippi",
        ["OK"] = "Oklahoma",
        ["KS"] = "Kansas",
        ["NE"] = "Nebraska",
        ["SD"] = "South Dakota",
        ["ND"] = "North Dakota",
        ["PA"] = "Pennsylvania",
        ["NJ"] = "New Jersey",
        ["DE"] = "Delaware",
        ["MD"] = "Maryland",
        ["MA"] = "Massachusetts",
        ["CT"] = "Connecticut",
        ["RI"] = "Rhode Island",
        ["VT"] = "Vermont",
        ["NH"] = "New Hampshire",
        ["ME"] = "Maine",
        ["AK"] = "Alaska",
        ["HI"] = "Hawaii"
    };

    var fixedCount = 0;
    
    foreach (var kvp in stateMapping)
    {
        var areas = await _db.Areas.Where(a => a.State == kvp.Key).ToListAsync();
        
        foreach (var area in areas)
        {
            area.State = kvp.Value;
            fixedCount++;
        }
    }
    
    await _db.SaveChangesAsync();
    
    return Ok(new { success = true, message = $"Fixed {fixedCount} areas", statesFixed = stateMapping.Count });
}

    /// <summary>
    /// Delete an area and all its children/climbs by name
    /// </summary>
    [HttpDelete("area/{areaName}")]
    public async Task<ActionResult> DeleteArea(string areaName)
    {
        var area = await _db.Areas
            .Include(a => a.Climbs)
            .FirstOrDefaultAsync(a => a.Name == areaName && a.ParentAreaId == null);
        
        if (area == null)
            return NotFound($"Area '{areaName}' not found");

        // Delete all climbs first
        _db.Climbs.RemoveRange(area.Climbs);
        
        // Delete child areas recursively
        await DeleteChildAreas(area.Id);
        
        // Delete the area itself
        _db.Areas.Remove(area);
        
        await _db.SaveChangesAsync();
        
        return Ok(new { 
            success = true,
            message = $"Deleted area '{areaName}' and all its children" 
        });
    }

    /// <summary>
    /// Delete ALL areas and climbs - use with caution!
    /// </summary>
    [HttpDelete("reset")]
    public async Task<ActionResult> ResetDatabase()
    {
        try
        {
            // Delete all climbs first (foreign key constraint)
            var allClimbs = await _db.Climbs.ToListAsync();
            _db.Climbs.RemoveRange(allClimbs);
            
            // Delete all areas
            var allAreas = await _db.Areas.ToListAsync();
            _db.Areas.RemoveRange(allAreas);
            
            await _db.SaveChangesAsync();
            
            return Ok(new { 
                success = true,
                message = $"Database reset complete. Deleted {allClimbs.Count} climbs and {allAreas.Count} areas." 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                success = false,
                message = $"Reset failed: {ex.Message}" 
            });
        }
    }

    private async Task DeleteChildAreas(int parentId)
    {
        var children = await _db.Areas
            .Include(a => a.Climbs)
            .Where(a => a.ParentAreaId == parentId)
            .ToListAsync();
        
        foreach (var child in children)
        {
            _db.Climbs.RemoveRange(child.Climbs);
            await DeleteChildAreas(child.Id);
            _db.Areas.Remove(child);
        }
    }
}

public class SearchImportRequest
{
    public string AreaName { get; set; } = null!;
}