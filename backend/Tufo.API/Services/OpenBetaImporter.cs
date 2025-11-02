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

    public async Task<ImportResult> ImportTexasRoutes()
    {
        var result = new ImportResult();

        // GraphQL query for Texas climbing areas and routes
        var query = new GraphQLRequest
        {
            Query = @"
                query TexasAreas {
                    areas(filter: { area_name: { match: ""Texas"" } }) {
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
            "
        };

        try
        {
            var response = await _client.SendQueryAsync<OpenBetaResponse>(query);

            if (response.Errors != null && response.Errors.Any())
            {
                result.Errors.AddRange(response.Errors.Select(e => e.Message));
                return result;
            }

            Console.WriteLine($"Found {response.Data.Areas.Count} areas from OpenBeta");

            foreach (var areaData in response.Data.Areas)
            {
                // Skip areas with no name
                if (string.IsNullOrWhiteSpace(areaData.AreaName))
                {
                    Console.WriteLine("Skipping area with null name");
                    continue;
                }

                Console.WriteLine($"Processing area: {areaData.AreaName} with {areaData.Climbs.Count} climbs");

                // Create or find area
                var area = await _db.Areas.FirstOrDefaultAsync(a => a.Name == areaData.AreaName);
                if (area == null)
                {
                    area = new Area
                    {
                        Name = areaData.AreaName,
                        State = "TX",
                        Country = "United States",
                        Lat = areaData.Metadata?.Lat,
                        Lng = areaData.Metadata?.Lng
                    };
                    _db.Areas.Add(area);
                    await _db.SaveChangesAsync();
                    result.AreasImported++;
                }

                // Import climbs (Sport and Boulder only)
                foreach (var climbData in areaData.Climbs)
                {
                    // Skip climbs with no name
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

                    // Determine primary type
                    string climbType = isSport ? "Sport" : "Boulder";

                    var climb = new Climb
                    {
                        AreaId = area.Id,
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

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
            return result;
        }
    }

    private static string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }
}

// DTOs for OpenBeta GraphQL response
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