using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Tufo.Core.Entities;
using Tufo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Tufo.API.Services;

public class OpenBetaImporter
{
    private readonly TufoContext _db;
    private readonly GraphQLHttpClient _client;

    public OpenBetaImporter(TufoContext db)
    {
        _db = db;
        _client = new GraphQLHttpClient("https://api.openbeta.io/graphql", new NewtonsoftJsonSerializer());
    }

    public async Task<ImportResult> ImportAreaByName(string areaName)
    {
        var result = new ImportResult();

        var query = new GraphQLRequest
        {
            Query = @"
                query SearchArea($name: String!) {
                    areas(filter: { area_name: { match: $name } }) {
                        area_name
                        uuid
                        metadata {
                            lat
                            lng
                        }
                        children {
                            area_name
                            uuid
                            metadata {
                                lat
                                lng
                            }
                            children {
                                area_name
                                uuid
                                metadata {
                                    lat
                                    lng
                                }
                                climbs {
                                    name
                                    uuid
                                    type {
                                        trad
                                        sport
                                        bouldering
                                        tr
                                    }
                                    grades {
                                        yds
                                    }
                                    metadata {
                                        lat
                                        lng
                                    }
                                    content {
                                        description
                                    }
                                }
                            }
                        }
                    }
                }
            ",
            Variables = new { name = areaName }
        };

        try
        {
            var response = await _client.SendQueryAsync<OpenBetaResponse>(query);

            if (response.Errors != null && response.Errors.Any())
            {
                result.Errors.AddRange(response.Errors.Select(e => e.Message));
                return result;
            }

            Console.WriteLine($"Found {response.Data.Areas.Count} top-level areas matching '{areaName}'");

            foreach (var topLevelData in response.Data.Areas)
            {
                if (string.IsNullOrWhiteSpace(topLevelData.AreaName))
                    continue;

                // Create or get top-level area (e.g., "Barton Creek Greenbelt")
                var topLevel = await GetOrCreateArea(topLevelData, null);
                if (topLevel != null)
                {
                    result.AreasImported++;
                    Console.WriteLine($"Top-level area: {topLevel.Name}");

                    // Process sub-areas (e.g., "Gus Fruh", "Maggie's Wall")
                    foreach (var subAreaData in topLevelData.Children)
                    {
                        if (string.IsNullOrWhiteSpace(subAreaData.AreaName))
                            continue;

                        var subArea = await GetOrCreateArea(subAreaData, topLevel.Id);
                        if (subArea != null)
                        {
                            result.AreasImported++;
                            Console.WriteLine($"  Sub-area: {subArea.Name}");

                            // Process walls (e.g., "Main Wall", "North Face")
                            foreach (var wallData in subAreaData.Children)
                            {
                                if (string.IsNullOrWhiteSpace(wallData.AreaName))
                                    continue;

                                var wall = await GetOrCreateArea(wallData, subArea.Id);
                                if (wall != null)
                                {
                                    result.AreasImported++;
                                    Console.WriteLine($"    Wall: {wall.Name} with {wallData.Climbs.Count} climbs");

                                    // Import climbs on this wall
                                    await ImportClimbs(wallData.Climbs, wall.Id, result);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
            return result;
        }
    }

    private async Task<Area?> GetOrCreateArea(AreaData areaData, int? parentId)
    {
        // Try to find existing by name and parent
        var existing = await _db.Areas
            .FirstOrDefaultAsync(a => a.Name == areaData.AreaName && a.ParentAreaId == parentId);

        if (existing != null)
            return existing;

        var area = new Area
        {
            Name = areaData.AreaName,
            State = "TX", //TODO: Make this dynamic based on query
            Country = "United States",
            Lat = areaData.Metadata?.Lat,
            Lng = areaData.Metadata?.Lng,
            ParentAreaId = parentId
        };

        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
        return area;
    }

    private async Task ImportClimbs(List<ClimbData> climbs, int areaId, ImportResult result)
    {
        foreach (var climbData in climbs)
        {
            if (string.IsNullOrWhiteSpace(climbData.Name))
                continue;

            // Filter: only sport and boulder
            var isSport = climbData.Type?.Sport ?? false;
            var isBoulder = climbData.Type?.Bouldering ?? false;

            if (!isSport && !isBoulder)
                continue;

            // Check if already exists
            var exists = await _db.Climbs.AnyAsync(c =>
                c.Source == "OpenBeta" && c.SourceId == climbData.Uuid);

            if (exists)
            {
                result.ClimbsSkipped++;
                continue;
            }

            string climbType = isSport ? "Sport" : "Boulder";

            var climb = new Climb
            {
                AreaId = areaId,
                Name = climbData.Name,
                Type = climbType,
                Yds = climbData.Grades?.Yds,
                Description = climbData.Content?.Description,
                Lat = climbData.Metadata?.Lat,
                Lng = climbData.Metadata?.Lng,
                Source = "OpenBeta",
                SourceId = climbData.Uuid
            };

            _db.Climbs.Add(climb);
            result.ClimbsImported++;
        }

        await _db.SaveChangesAsync();
    }

    // Keep the old Texas-specific method for backwards compatibility
    public async Task<ImportResult> ImportTexasRoutes()
    {
        return await ImportAreaByName("Texas");
    }
}

// DTOs
public class OpenBetaResponse
{
    public List<AreaData> Areas { get; set; } = new();
}

public class AreaData
{
    [JsonProperty("area_name")]
    public string AreaName { get; set; } = null!;

    [JsonProperty("uuid")]
    public string Uuid { get; set; } = null!;

    [JsonProperty("metadata")]
    public AreaMetadata? Metadata { get; set; }

    [JsonProperty("children")]
    public List<AreaData> Children { get; set; } = new();

    [JsonProperty("climbs")]
    public List<ClimbData> Climbs { get; set; } = new();
}

public class AreaMetadata
{
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

public class ClimbData
{
    public string Name { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public ClimbTypeData? Type { get; set; }
    public GradeData? Grades { get; set; }
    public ClimbMetadata? Metadata { get; set; }
    public ContentData? Content { get; set; }
}

public class ClimbTypeData
{
    public bool? Trad { get; set; }
    public bool? Sport { get; set; }
    public bool? Bouldering { get; set; }
    public bool? Tr { get; set; }
}

public class GradeData
{
    public string? Yds { get; set; }
}

public class ClimbMetadata
{
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

public class ContentData
{
    public string? Description { get; set; }
}

public class ImportResult
{
    public int AreasImported { get; set; }
    public int ClimbsImported { get; set; }
    public int ClimbsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();

    public bool Success => !Errors.Any();
}