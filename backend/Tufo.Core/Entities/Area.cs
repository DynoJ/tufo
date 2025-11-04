namespace Tufo.Core.Entities;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? State { get; set; }
    public string Country { get; set; } = "United States";
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // Hierarchy support - allows Area -> SubArea -> Wall structure
    public int? ParentAreaId { get; set; }
    public Area? ParentArea { get; set; }
    public ICollection<Area> SubAreas { get; set; } = new List<Area>();

    public ICollection<Climb> Climbs { get; set; } = new List<Climb>();
}