using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tufo.API.Services;

namespace Tufo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly OpenBetaImporter _importer;

    public ImportController(OpenBetaImporter importer)
    {
        _importer = importer;
    }

    /// <summary>
    /// Import Texas sport and boulder routes from OpenBeta
    /// </summary>
    [HttpPost("texas")]
    public async Task<ActionResult<ImportResult>> ImportTexas()
    {
        var result = await _importer.ImportTexasRoutes();

        if (!result.Success)
            return BadRequest(result);

        return Ok(new
        {
            success = true,
            message = $"Imported {result.ClimbsImported} climbs across {result.AreasImported} areas. Skipped {result.ClimbsSkipped} duplicates.",
            result.AreasImported,
            result.ClimbsImported,
            result.ClimbsSkipped
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
}

public class SearchImportRequest
{
    public string AreaName { get; set; } = null!;
}