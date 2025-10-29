namespace Tufo.Core.Entities;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? State { get; set; }
    public string Country { get; set; } = "United States";
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    public ICollection<Climb> Climbs { get; set; } = new List<Climb>();
}