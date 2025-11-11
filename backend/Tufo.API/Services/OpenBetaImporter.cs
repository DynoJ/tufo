using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tufo.Infrastructure;
using Tufo.Core.Entities;
using Tufo.API.Models;

namespace Tufo.API.Services;

public class OpenBetaImporter
{
    private readonly HttpClient _http;
    private readonly TufoContext _db;
    private readonly ILogger<OpenBetaImporter> _log;
    private readonly TimeSpan _betweenRequests = TimeSpan.FromSeconds(1.0);
    private readonly int _maxRetries = 3;

    public OpenBetaImporter(HttpClient httpClient, TufoContext db, ILogger<OpenBetaImporter> log)
    {
        _http = httpClient;
        _db = db;
        _log = log;

        // ensure JSON accept header
        if (!_http.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Public entry point: import a single state (e.g., "Utah") or call ImportAllStatesAsync()
    public async Task ImportAllStatesAsync(CancellationToken ct = default)
    {
        var states = new[] {
            "Alabama","Alaska","Arizona","Arkansas","California","Colorado","Connecticut","Delaware",
            "Florida","Georgia","Hawaii","Idaho","Illinois","Indiana","Iowa","Kansas","Kentucky",
            "Louisiana","Maine","Maryland","Massachusetts","Michigan","Minnesota","Mississippi",
            "Missouri","Montana","Nebraska","Nevada","New Hampshire","New Jersey","New Mexico",
            "New York","North Carolina","North Dakota","Ohio","Oklahoma","Oregon","Pennsylvania",
            "Rhode Island","South Carolina","South Dakota","Tennessee","Texas","Utah","Vermont",
            "Virginia","Washington","West Virginia","Wisconsin","Wyoming"
        };

        _log.LogInformation("Starting full US import ({count} states)", states.Length);

        foreach (var s in states)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _log.LogInformation("Importing state {state}", s);
                await ImportStateAsync(s, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to import state {state}", s);
            }
        }

        _log.LogInformation("All states import finished.");
    }

    // Import by state name
    public async Task ImportStateAsync(string state, CancellationToken ct = default)
    {
        var query = GetSearchAreasQuery();
        var variables = new { stateName = state };

        var doc = await ExecuteGraphQLAsync(query, variables, ct);
        if (doc == null)
        {
            _log.LogWarning("Empty response for state {state}", state);
            return;
        }

        if (!doc.RootElement.TryGetProperty("data", out var data))
        {
            _log.LogWarning("Unexpected response shape (no data) for state {state}. Dumping root.", state);
            _log.LogDebug(doc.RootElement.ToString());
            return;
        }

        // OpenBeta returns data.areas directly (not edges/nodes)
        if (!data.TryGetProperty("areas", out var areas) || areas.GetArrayLength() == 0)
        {
            _log.LogInformation("No areas returned for {state}.", state);
            return;
        }

        foreach (var areaNode in areas.EnumerateArray())
        {
            await ProcessAreaNodeAsync(areaNode, state, ct);
        }

        await Task.Delay(_betweenRequests, ct);
    }

    // ---- Core processing ----
    private async Task ProcessAreaNodeAsync(JsonElement node, string state, CancellationToken ct)
    {
        // Map node -> Area entity
        var externalId = node.GetStringOrNull("uuid");
        var name = node.GetStringOrNull("area_name") ?? "Unknown Area";
        
        // Get coordinates from metadata
        double? lat = null;
        double? lng = null;
        if (node.TryGetProperty("metadata", out var metadata))
        {
            lat = metadata.GetNullableDouble("lat");
            lng = metadata.GetNullableDouble("lng");
        }

        // We'll attempt to find existing area by Name + State + ParentAreaId = null (top-level). This simple heuristic reduces duplicates.
        var existing = await _db.Areas.FirstOrDefaultAsync(a =>
            a.Name == name && a.State == state && a.ParentAreaId == null, ct);

        Area area;
        if (existing == null)
        {
            area = new Area
            {
                Name = name,
                State = state,
                Lat = lat,
                Lng = lng
            };
            _db.Areas.Add(area);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created Area {areaId}:{name}", area.Id, area.Name);
        }
        else
        {
            area = existing;
            // update coords if missing
            var changed = false;
            if (!area.Lat.HasValue && lat.HasValue) { area.Lat = lat; changed = true; }
            if (!area.Lng.HasValue && lng.HasValue) { area.Lng = lng; changed = true; }
            if (changed) { _db.Areas.Update(area); await _db.SaveChangesAsync(ct); _log.LogDebug("Updated coords for Area {id}", area.Id); }
        }

        // Process children (subareas/crags)
        if (node.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                await ProcessSubAreaNodeAsync(child, state, area, ct);
            }
        }

        // If climbs are embedded directly on area node:
        if (node.TryGetProperty("climbs", out var climbs))
        {
            foreach (var climb in climbs.EnumerateArray())
            {
                await ProcessRouteNodeAsync(climb, area, ct);
            }
        }
    }

    private async Task ProcessSubAreaNodeAsync(JsonElement node, string state, Area parent, CancellationToken ct)
    {
        var name = node.GetStringOrNull("area_name") ?? "Subarea";
        
        // Get coordinates from metadata
        double? lat = null;
        double? lng = null;
        if (node.TryGetProperty("metadata", out var metadata))
        {
            lat = metadata.GetNullableDouble("lat");
            lng = metadata.GetNullableDouble("lng");
        }

        // Find or create subarea by name + parent id
        var existing = await _db.Areas.FirstOrDefaultAsync(a =>
            a.Name == name && a.ParentAreaId == parent.Id && a.State == state, ct);

        Area sub;
        if (existing == null)
        {
            sub = new Area
            {
                Name = name,
                State = state,
                ParentAreaId = parent.Id,
                Lat = lat,
                Lng = lng
            };
            _db.Areas.Add(sub);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created SubArea {id}:{name} under Area {parent}", sub.Id, sub.Name, parent.Id);
        }
        else
        {
            sub = existing;
        }

        // climbs under this subarea
        if (node.TryGetProperty("climbs", out var climbs))
        {
            foreach (var climb in climbs.EnumerateArray())
            {
                await ProcessRouteNodeAsync(climb, sub, ct);
            }
        }

        // recursively handle deeper children
        if (node.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                await ProcessSubAreaNodeAsync(child, state, sub, ct);
        }
    }

    private async Task ProcessRouteNodeAsync(JsonElement routeNode, Area area, CancellationToken ct)
    {
        // Extract fields defensively
        var extId = routeNode.GetStringOrNull("uuid") ?? routeNode.GetStringOrNull("id");
        if (string.IsNullOrEmpty(extId))
        {
            _log.LogWarning("Skipping route with no external id in area {area}", area.Name);
            return;
        }

        var name = routeNode.GetStringOrNull("name") ?? "Unnamed Route";
        
        // Parse type object {trad: bool, sport: bool, bouldering: bool}
        string climbType = "Sport";
        if (routeNode.TryGetProperty("type", out var typeObj))
        {
            var isTrad = typeObj.GetBoolOrNull("trad") ?? false;
            var isSport = typeObj.GetBoolOrNull("sport") ?? false;
            var isBoulder = typeObj.GetBoolOrNull("bouldering") ?? false;
            
            if (isBoulder) climbType = "Boulder";
            else if (isTrad) climbType = "Trad";
            else if (isSport) climbType = "Sport";
        }

        // Get grade from grades.yds
        string? grade = null;
        if (routeNode.TryGetProperty("grades", out var grades))
        {
            grade = grades.GetStringOrNull("yds");
        }

        // Get description from content.description
        string? desc = null;
        if (routeNode.TryGetProperty("content", out var content))
        {
            desc = content.GetStringOrNull("description");
        }

        // Get coordinates from metadata
        double? lat = null;
        double? lng = null;
        if (routeNode.TryGetProperty("metadata", out var metadata))
        {
            lat = metadata.GetNullableDouble("lat");
            lng = metadata.GetNullableDouble("lng");
        }

        // Get hero image from media array
        string? heroUrl = null;
        if (routeNode.TryGetProperty("media", out var mediaArray) && 
            mediaArray.ValueKind == JsonValueKind.Array && 
            mediaArray.GetArrayLength() > 0)
        {
            heroUrl = mediaArray[0].GetStringOrNull("mediaUrl");
        }

        // upsert by Source + SourceId
        var existing = await _db.Climbs.FirstOrDefaultAsync(c =>
            c.Source == "OpenBeta" && c.SourceId == extId, ct);

        if (existing == null)
        {
            var climb = new Climb
            {
                AreaId = area.Id,
                Name = name,
                Type = climbType,
                Yds = grade,
                Description = desc,
                Lat = lat,
                Lng = lng,
                HeroUrl = heroUrl,
                Source = "OpenBeta",
                SourceId = extId
            };
            _db.Climbs.Add(climb);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Inserted climb {name} (id:{id}) from OpenBeta", climb.Name, climb.Id);
        }
        else
        {
            // update useful fields if changed
            var changed = false;
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.Type != climbType) { existing.Type = climbType; changed = true; }
            if (existing.Yds != grade) { existing.Yds = grade; changed = true; }
            if (existing.Description != desc) { existing.Description = desc; changed = true; }
            if (!existing.Lat.HasValue && lat.HasValue) { existing.Lat = lat; changed = true; }
            if (!existing.Lng.HasValue && lng.HasValue) { existing.Lng = lng; changed = true; }
            if (string.IsNullOrEmpty(existing.HeroUrl) && !string.IsNullOrEmpty(heroUrl)) { existing.HeroUrl = heroUrl; changed = true; }

            if (changed)
            {
                _db.Climbs.Update(existing);
                await _db.SaveChangesAsync(ct);
                _log.LogDebug("Updated climb {id} from OpenBeta", existing.Id);
            }
        }
    }

    // ---- HTTP + GraphQL helpers ----
    private async Task<JsonDocument?> ExecuteGraphQLAsync(string query, object variables, CancellationToken ct)
    {
        int attempt = 0;
        Exception? lastEx = null;

        while (attempt < _maxRetries)
        {
            attempt++;
            try
            {
                var payload = new { query, variables };
                var payloadJson = JsonSerializer.Serialize(payload);
                using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                using var res = await _http.PostAsync("/graphql", content, ct);
                var body = await res.Content.ReadAsStringAsync(ct);

                if (!res.IsSuccessStatusCode)
                {
                    _log.LogWarning("GraphQL returned {code}: {body}", res.StatusCode, Truncate(body, 500));
                    res.EnsureSuccessStatusCode();
                }

                return JsonDocument.Parse(body);
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                lastEx = ex;
                var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _log.LogWarning(ex, "GraphQL attempt {attempt} failed. Retrying in {backoff}s", attempt, backoff.TotalSeconds);
                await Task.Delay(backoff, ct);
            }
        }

        if (lastEx != null) _log.LogError(lastEx, "GraphQL failed after {attempts} attempts", _maxRetries);
        return null;
    }

    private static string GetSearchAreasQuery()
    {
        return @"
query SearchAreas($stateName: String!) {
  areas(filter: {area_name: {match: $stateName}}) {
    uuid
    area_name
    metadata {
      lat
      lng
    }
    children {
      uuid
      area_name
      metadata {
        lat
        lng
      }
      climbs {
        uuid
        name
        type {
          trad
          sport
          bouldering
        }
        grades {
          yds
        }
        content {
          description
        }
        metadata {
          lat
          lng
        }
        media {
          mediaUrl
        }
      }
    }
    climbs {
      uuid
      name
      type {
        trad
        sport
        bouldering
      }
      grades {
        yds
      }
      content {
        description
      }
      metadata {
        lat
        lng
      }
      media {
        mediaUrl
      }
    }
  }
}
";
    }

    private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));

    /// <summary>
    /// Import all 50 US states (sport and boulder routes only)
    /// Returns detailed result for API response
    /// </summary>
    public async Task<ImportResult> ImportAllUSStates(CancellationToken ct = default)
    {
        var result = new ImportResult { Success = true };
        
        var states = new[] {
            "Alabama","Alaska","Arizona","Arkansas","California","Colorado","Connecticut","Delaware",
            "Florida","Georgia","Hawaii","Idaho","Illinois","Indiana","Iowa","Kansas","Kentucky",
            "Louisiana","Maine","Maryland","Massachusetts","Michigan","Minnesota","Mississippi",
            "Missouri","Montana","Nebraska","Nevada","New Hampshire","New Jersey","New Mexico",
            "New York","North Carolina","North Dakota","Ohio","Oklahoma","Oregon","Pennsylvania",
            "Rhode Island","South Carolina","South Dakota","Tennessee","Texas","Utah","Vermont",
            "Virginia","Washington","West Virginia","Wisconsin","Wyoming"
        };
        
        try
        {
            _log.LogInformation("Starting ALL US STATES import ({count} states)", states.Length);
            
            var initialCount = await _db.Climbs.CountAsync(ct);
            var initialAreaCount = await _db.Areas.CountAsync(ct);
            
            foreach (var state in states)
            {
                try
                {
                    _log.LogInformation("Importing state: {state}", state);
                    await ImportStateAsync(state, ct);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{state}: {ex.Message}");
                    _log.LogError(ex, "Failed to import {state}", state);
                }
            }
            
            var finalCount = await _db.Climbs.CountAsync(ct);
            var finalAreaCount = await _db.Areas.CountAsync(ct);
            
            result.ClimbsImported = finalCount - initialCount;
            result.AreasImported = finalAreaCount - initialAreaCount;
            result.Message = $"Imported {result.ClimbsImported} climbs across {result.AreasImported} areas from {states.Length} states";
            
            _log.LogInformation("All states import complete: {message}", result.Message);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.Message);
            _log.LogError(ex, "Failed to import all states");
        }
        
        return result;
    }

    /// <summary>
    /// Import a specific area by name - returns detailed result
    /// </summary>
    public async Task<ImportResult> ImportAreaByName(string areaName, CancellationToken ct = default)
    {
        var result = new ImportResult { Success = true };
        
        try
        {
            _log.LogInformation("Starting import for area: {areaName}", areaName);
            
            var initialCount = await _db.Climbs.CountAsync(ct);
            var initialAreaCount = await _db.Areas.CountAsync(ct);
            
            // Search for the specific area
            var query = GetSearchAreasQuery();
            var variables = new { stateName = areaName };
            
            var doc = await ExecuteGraphQLAsync(query, variables, ct);
            if (doc == null)
            {
                result.Success = false;
                result.Errors.Add($"No results found for '{areaName}'");
                return result;
            }

            if (!doc.RootElement.TryGetProperty("data", out var data))
            {
                result.Success = false;
                result.Errors.Add("Invalid response from OpenBeta API");
                return result;
            }

            if (!data.TryGetProperty("areas", out var areas) || areas.GetArrayLength() == 0)
            {
                result.Success = false;
                result.Errors.Add($"No areas found for '{areaName}'");
                return result;
            }

            // Process the first matching area
            foreach (var areaNode in areas.EnumerateArray())
            {
                var state = "Unknown";
                await ProcessAreaNodeAsync(areaNode, state, ct);
                break;
            }
            
            var finalCount = await _db.Climbs.CountAsync(ct);
            var finalAreaCount = await _db.Areas.CountAsync(ct);
            
            result.ClimbsImported = finalCount - initialCount;
            result.AreasImported = finalAreaCount - initialAreaCount;
            result.Message = $"Imported {result.ClimbsImported} climbs across {result.AreasImported} areas";
            
            _log.LogInformation("Area import complete: {message}", result.Message);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.Message);
            _log.LogError(ex, "Failed to import area {areaName}", areaName);
        }
        
        return result;
    }
}

// ---- JsonElement helpers ----
static class JsonElementHelpers
{
    public static string? GetStringOrNull(this JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        if (el.TryGetProperty(prop, out var p) && p.ValueKind != JsonValueKind.Null)
            return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
        return null;
    }

    public static double? GetNullableDouble(this JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        if (el.TryGetProperty(prop, out var p) && p.ValueKind != JsonValueKind.Null)
        {
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var d)) return d;
            if (p.ValueKind == JsonValueKind.String && double.TryParse(p.GetString(), out var parsed)) return parsed;
        }
        return null;
    }

    public static bool? GetBoolOrNull(this JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        if (el.TryGetProperty(prop, out var p))
        {
            if (p.ValueKind == JsonValueKind.True) return true;
            if (p.ValueKind == JsonValueKind.False) return false;
        }
        return null;
    }
}