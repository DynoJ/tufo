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

    [HttpGet] public Task<List<Area>> GetAll() =>
        _db.Areas.AsNoTracking().ToListAsync();

    [HttpPost] public async Task<ActionResult<Area>> Create(Area area)
    {
        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = area.Id }, area);
    }
}