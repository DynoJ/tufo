using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tufo.Infrastructure;

namespace Tufo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly TufoContext _db;

        public SearchController(TufoContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Hierarchical search - searches states, areas at all levels, and climbs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<SearchResultDto>());

            var query = q.Trim().ToLower();
            var results = new List<SearchResultDto>();

            // 1. Search by STATE - returns top-level areas in that state
            var stateResults = await _db.Areas
                .Where(a => a.State != null && 
                           a.State.ToLower().Contains(query) &&
                           a.ParentAreaId == null)
                .Take(5)
                .ToListAsync();

            foreach (var area in stateResults)
            {
                var climbCount = await GetTotalClimbCountRecursive(area.Id);
                results.Add(new SearchResultDto
                {
                    Id = area.Id,
                    Name = area.Name,
                    Type = "area",
                    Location = area.State,
                    Hierarchy = "State → Area",
                    ClimbCount = climbCount
                });
            }

            // 2. Search ALL AREAS (any level in hierarchy) by name
            var areaResults = await _db.Areas
                .Where(a => a.Name.ToLower().Contains(query))
                .Take(10)
                .ToListAsync();

            foreach (var area in areaResults)
            {
                var breadcrumb = await GetAreaBreadcrumb(area.Id);
                var climbCount = await GetTotalClimbCountRecursive(area.Id);
                
                results.Add(new SearchResultDto
                {
                    Id = area.Id,
                    Name = area.Name,
                    Type = "area",
                    Location = breadcrumb,
                    Hierarchy = area.ParentAreaId == null ? "Top-Level" : "Sub-Area",
                    ClimbCount = climbCount
                });
            }

            // 3. Search CLIMBS by name
            var climbResults = await _db.Climbs
                .Include(c => c.Area)
                .Where(c => c.Name.ToLower().Contains(query))
                .Take(10)
                .ToListAsync();

            foreach (var climb in climbResults)
            {
                var breadcrumb = climb.Area != null 
                    ? await GetAreaBreadcrumb(climb.Area.Id) 
                    : "Unknown";

                results.Add(new SearchResultDto
                {
                    Id = climb.Id,
                    Name = climb.Name,
                    Type = "climb",
                    Grade = climb.Yds,
                    Location = breadcrumb,
                    Hierarchy = "Route"
                });
            }

            // Remove duplicates
            var uniqueResults = results
                .GroupBy(r => new { r.Id, r.Type })
                .Select(g => g.First())
                .Take(20)
                .ToList();

            return Ok(uniqueResults);
        }

        /// <summary>
        /// Get breadcrumb path for an area (e.g., "Texas > Barton Creek Greenbelt > Gus Fruh")
        /// </summary>
        private async Task<string> GetAreaBreadcrumb(int areaId)
        {
            var breadcrumbs = new List<string>();
            int? currentId = areaId;

            while (currentId != null)
            {
                var area = await _db.Areas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == currentId);

                if (area == null) break;

                breadcrumbs.Insert(0, area.Name);
                currentId = area.ParentAreaId;
            }

            return string.Join(" > ", breadcrumbs);
        }

        /// <summary>
        /// Count all climbs in an area and its sub-areas recursively
        /// </summary>
        private async Task<int> GetTotalClimbCountRecursive(int areaId)
        {
            // Direct climbs
            var directCount = await _db.Climbs.CountAsync(c => c.AreaId == areaId);

            // Sub-area climbs
            var subAreaIds = await _db.Areas
                .Where(a => a.ParentAreaId == areaId)
                .Select(a => a.Id)
                .ToListAsync();

            var subCount = 0;
            foreach (var subId in subAreaIds)
            {
                subCount += await GetTotalClimbCountRecursive(subId);
            }

            return directCount + subCount;
        }
    }

    public class SearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = ""; // "area" or "climb"
        public string? Location { get; set; }
        public string? Grade { get; set; }
        public int? ClimbCount { get; set; }
        public string? Hierarchy { get; set; }
    }
}