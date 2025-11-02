using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Tufo.Core.Entities;
using Tufo.Infrastructure;
using FFMpegCore;

namespace Tufo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClimbsController : ControllerBase
{
    private readonly TufoContext _db;
    private readonly IWebHostEnvironment _env;

    public ClimbsController(TufoContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ========== GET all climbs ==========
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Climb>>> GetAll()
    {
        var climbs = await _db.Climbs
            .Include(c => c.Area)
            .Include(c => c.Media)
            .Include(c => c.Notes)
            .AsNoTracking()
            .ToListAsync();

        return Ok(climbs);
    }

    // ========== GET single climb ==========
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Climb>> GetOne(int id)
    {
        var climb = await _db.Climbs
            .Include(c => c.Area)
            .Include(c => c.Media)
            .Include(c => c.Notes.OrderByDescending(n => n.CreatedAt))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return climb is null ? NotFound() : Ok(climb);
    }

    // ========== POST create climb ==========
    [HttpPost]
    public async Task<ActionResult<Climb>> Create([FromBody] Climb climb)
    {
        _db.Climbs.Add(climb);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOne), new { id = climb.Id }, climb);
    }

    // ========== POST add a note ==========
    [HttpPost("{id:int}/notes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RouteNote>> AddNote(int id, [FromBody] RouteNote note)
    {
        if (!await _db.Climbs.AnyAsync(c => c.Id == id)) return NotFound();
        note.ClimbId = id;
        _db.RouteNotes.Add(note);
        await _db.SaveChangesAsync();
        return Created($"/api/climbs/{id}", note);
    }

    // ========== POST upload media (image or video) ==========
    [HttpPost("{id:int}/media")]
    [Authorize]
    [RequestSizeLimit(100_000_000)] // 100MB hard cap
    public async Task<ActionResult<Media>> UploadMedia(int id, IFormFile file, [FromForm] string? caption)
    {
        if (!await _db.Climbs.AnyAsync(c => c.Id == id)) return NotFound();
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

        var uploads = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fname = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploads, fname);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        bool isImage = contentType.StartsWith("image/") || new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext);
        bool isVideo = contentType.StartsWith("video/") || new[] { ".mp4", ".webm", ".mov" }.Contains(ext);

        if (!isImage && !isVideo)
        {
            System.IO.File.Delete(fullPath);
            return BadRequest("Only images (jpg/png/webp) or videos (mp4/webm/mov) are allowed.");
        }

        var media = new Media
        {
            ClimbId = id,
            Caption = caption,
            Url = $"/uploads/{fname}",
            Bytes = file.Length,
            Type = isImage ? MediaType.Photo : MediaType.Video
        };

        if (isVideo)
        {
            try
            {
                var info = await FFProbe.AnalyseAsync(fullPath);
                var dur = (int)Math.Round(info.Duration.TotalSeconds);
                if (dur > 60)
                {
                    System.IO.File.Delete(fullPath);
                    return BadRequest("Video must be 60 seconds or less.");
                }
                media.DurationSeconds = dur;

                var thumbName = $"{Path.GetFileNameWithoutExtension(fname)}.jpg";
                var thumbFull = Path.Combine(uploads, thumbName);

                await FFMpeg.SnapshotAsync(
                    fullPath,
                    thumbFull,
                    null, // no forced resize
                    TimeSpan.FromSeconds(Math.Min(1, dur))
                );

                media.ThumbnailUrl = $"/uploads/{thumbName}";
            }
            catch
            {
                System.IO.File.Delete(fullPath);
                return BadRequest("Could not analyse video. Ensure FFmpeg is installed.");
            }
        }

        _db.Media.Add(media);
        await _db.SaveChangesAsync();
        return Created($"/api/climbs/{id}", media);
    }
}