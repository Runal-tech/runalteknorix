using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeknorixJobAPI.Models;
using System.ComponentModel.DataAnnotations;
using TeknorixJobAPI.Helper;

namespace TeknorixJobAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public record JobCreateRequestDto(
            [Required] string Title,
            [Required] string Description,
            [Required] int LocationId,
            [Required] int DepartmentId,
            [Required] DateTime ClosingDate
        );

        public record JobUpdateRequestDto(
            [Required] string Title,
            [Required] string Description,
            [Required] int LocationId,
            [Required] int DepartmentId,
            [Required] DateTime ClosingDate
        );

        public record JobListRequestDto(
            string? Q,
            int PageNo = 1,
            int PageSize = 10,
            int? LocationId = null,
            int? DepartmentId = null
        );

        public record JobListResponseDto(
            int Total,
            IEnumerable<JobListItemDto> Data
        );

        public record JobListItemDto(
            int Id,
            string Code,
            string Title,
            string Location, 
            string Department, 
            DateTime PostedDate,
            DateTime ClosingDate
        );

        public record JobDetailDto(
            int Id,
            string Code,
            string Title,
            string Description,
            LocationDetailDto Location, 
            DepartmentDetailDto Department, 
            DateTime PostedDate,
            DateTime ClosingDate
        );

        public record LocationDetailDto(
            int Id,
            string Title,
            string City,
            string State,
            string Country,
            string Zip
        );

        public record DepartmentDetailDto(
            int Id,
            string Title
        );

        // --- API Endpoints ---

        /// <summary>
        /// Creates a new job opening.
        /// </summary>
        /// <param name="request">Job creation details.</param>
        /// <returns>A newly created job opening.</returns>
        /// <response code="201">Returns the URL of the newly created item.</response>
        /// <response code="400">If the request is invalid or referenced IDs do not exist.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateJob([FromBody] JobCreateRequestDto request)
        {
            var locationExists = await _context.Locations.AnyAsync(l => l.Id == request.LocationId);
            if (!locationExists)
            {
                return BadRequest($"Location with ID {request.LocationId} does not exist.");
            }

            var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
            if (!departmentExists)
            {
                return BadRequest($"Department with ID {request.DepartmentId} does not exist.");
            }

            var newJob = new Job
            {
                Title = request.Title,
                Description = request.Description,
                LocationId = request.LocationId,
                DepartmentId = request.DepartmentId,
                ClosingDate = request.ClosingDate.ToUniversalTime(), 
                PostedDate = DateTime.UtcNow, 
                Code = $"JOB-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}" 
            };

            _context.Jobs.Add(newJob);
            await _context.SaveChangesAsync();

            var newJobId = newJob.Id;
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString();
            if (string.IsNullOrEmpty(apiVersion))
            {
                apiVersion = "1"; 
            }
            else if (!apiVersion.StartsWith("v"))
            {
                apiVersion = $"v{apiVersion}"; 
            }

            var locationUrl = $"{baseUrl}/api/{apiVersion}/jobs/{newJobId}";

            return Created(locationUrl, null);
        }

        /// <summary>
        /// Updates an existing job opening.
        /// </summary>
        /// <param name="id">The ID of the job to update.</param>
        /// <param name="request">Job update details.</param>
        /// <returns>No content on successful update.</returns>
        /// <response code="200">If the job was successfully updated.</response>
        /// <response code="400">If the request is invalid or referenced IDs do not exist.</response>
        /// <response code="404">If the job with the specified ID is not found.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] JobUpdateRequestDto request)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            // Validate LocationId and DepartmentId exist if they are being updated
            if (job.LocationId != request.LocationId)
            {
                var locationExists = await _context.Locations.AnyAsync(l => l.Id == request.LocationId);
                if (!locationExists)
                {
                    return BadRequest($"Location with ID {request.LocationId} does not exist.");
                }
            }

            if (job.DepartmentId != request.DepartmentId)
            {
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
                if (!departmentExists)
                {
                    return BadRequest($"Department with ID {request.DepartmentId} does not exist.");
                }
            }

            // Update job properties
            job.Title = request.Title;
            job.Description = request.Description;
            job.LocationId = request.LocationId;
            job.DepartmentId = request.DepartmentId;
            job.ClosingDate = request.ClosingDate.ToUniversalTime(); // Store in UTC

            await _context.SaveChangesAsync();

            return Ok(); // 200 OK
        }

        /// <summary>
        /// Retrieves a list of job openings with optional filtering and pagination.
        /// </summary>
        /// <param name="request">Filter and pagination criteria.</param>
        /// <returns>A paginated list of job openings.</returns>
        /// <response code="200">Returns the list of job openings.</response>
        [HttpPost]
        [Route("/api/[controller]/list")] // Specific route as per requirements, no versioning here
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JobListResponseDto>> ListJobs([FromBody] JobListRequestDto request)
        {
            var query = _context.Jobs
                .Include(j => j.Location)
                .Include(j => j.Department)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Q))
            {
                query = query.Where(j =>
                    j.Title.Contains(request.Q) ||
                    j.Description.Contains(request.Q));
            }

            if (request.LocationId.HasValue)
            {
                query = query.Where(j => j.LocationId == request.LocationId.Value);
            }

            if (request.DepartmentId.HasValue)
            {
                query = query.Where(j => j.DepartmentId == request.DepartmentId.Value);
            }

            var total = await query.CountAsync();

            // Apply pagination
            var jobs = await query
                .OrderByDescending(j => j.PostedDate) // Or any desired order
                .Skip((request.PageNo - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var data = jobs.Select(j => new JobListItemDto(
                j.Id,
                j.Code,
                j.Title,
                j.Location?.Title ?? "N/A", // Handle null if location is not loaded (though it should be with Include)
                j.Department?.Title ?? "N/A", // Handle null if department is not loaded
                j.PostedDate,
                j.ClosingDate
            )).ToList();

            return Ok(new JobListResponseDto(total, data));
        }

        /// <summary>
        /// Retrieves detailed information for a specific job opening.
        /// </summary>
        /// <param name="id">The ID of the job to retrieve.</param>
        /// <returns>Detailed information about the job.</returns>
        /// <response code="200">Returns the detailed job information.</response>
        /// <response code="404">If the job with the specified ID is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobDetailDto>> GetJobById(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Location)
                .Include(j => j.Department)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            var jobDetail = new JobDetailDto(
                job.Id,
                job.Code,
                job.Title,
                job.Description,
                new LocationDetailDto(
                    job.Location?.Id ?? 0,
                    job.Location?.Title ?? "N/A",
                    job.Location?.City ?? "N/A",
                    job.Location?.State ?? "N/A",
                    job.Location?.Country ?? "N/A",
                    job.Location?.Zip ?? "N/A"
                ),
                new DepartmentDetailDto(
                    job.Department?.Id ?? 0,
                    job.Department?.Title ?? "N/A"
                ),
                job.PostedDate,
                job.ClosingDate
            );

            return Ok(jobDetail);
        }
    }
}