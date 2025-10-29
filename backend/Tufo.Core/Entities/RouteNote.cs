namespace Tufo.Core.Entities;

public class RouteNote
{
    public int Id { get; set; }
    public int ClimbId { get; set; }
    public Climb? Climb { get; set; }

    public string? UserId { get; set; }   // tie to user later
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}