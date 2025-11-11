namespace Tufo.Core.Models.OpenBeta
{
    public class OpenBetaArea
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentId { get; set; } // for hierarchy
        public string? Description { get; set; }
        public string? State { get; set; }
        public double[]? Coordinates { get; set; } // [lng, lat]
        public List<OpenBetaCrag> Crags { get; set; } = new();
    }

    public class OpenBetaCrag
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public string? Description { get; set; }
        public double[]? Coordinates { get; set; }
        public List<OpenBetaRoute> Routes { get; set; } = new();
    }

    public class OpenBetaRoute
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Grade { get; set; }
        public string? Type { get; set; } // e.g., "boulder", "sport", "trad"
        public string? Description { get; set; }
        public double? Length { get; set; } // meters
        public double[]? Coordinates { get; set; } // [lng, lat]
        public string? Safety { get; set; }
        public string? Rating { get; set; } // e.g., "V5", "5.12a"
        public string? AreaId { get; set; }
        public string? CragId { get; set; }
    }
}