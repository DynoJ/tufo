namespace Tufo.Core.Entities;

public class Climb
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public Area? Area { get; set; }

    public string Name { get; set; } = null!;
    public string Type { get; set; } = "Sport"; // Sport/Trad/Boulder
    public string? Yds { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public int? LengthMeters { get; set; }

    public string? Description { get; set; }

    // seeded overhead/hero image (like MP)
    public string? HeroUrl { get; set; }
    public string? HeroAttribution { get; set; }

    // optional source for seeded data
    public string? Source { get; set; }
    public string? SourceId { get; set; }

    public ICollection<Media> Media { get; set; } = new List<Media>();
    public ICollection<RouteNote> Notes { get; set; } = new List<RouteNote>();
}