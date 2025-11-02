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
}