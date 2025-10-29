namespace Tufo.Core.Entities;

public enum MediaType { Photo = 0, Video = 1 }

public class Media
{
    public int Id { get; set; }
    public int ClimbId { get; set; }
    public Climb? Climb { get; set; }

    public string? UserId { get; set; } // FK to ApplicationUser (add later when auth lands)

    public MediaType Type { get; set; } = MediaType.Photo;
    public string Url { get; set; } = null!;         // /uploads/...
    public string? ThumbnailUrl { get; set; }        // for video previews
    public string? Caption { get; set; }

    public int? DurationSeconds { get; set; }        // for videos
    public long? Bytes { get; set; }                 // optional sanity/debug
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}