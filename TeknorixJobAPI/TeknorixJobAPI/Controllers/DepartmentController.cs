using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeknorixJobAPI.Helper;
using TeknorixJobAPI.Models;

namespace TeknorixJobAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")] 
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public record DepartmentCreateRequestDto(
            [Required] string Title
        );

        public record DepartmentUpdateRequestDto(
            [Required] string Title
        );

        public record DepartmentResponseDto(
            int Id,
            string Title
        );

        // --- API Endpoints ---

        /// <summary>
        /// Creates a new department.
        /// </summary>
        /// <param name="request">Department creation details.</param>
        /// <returns>A newly created department.</returns>
        /// <response code="201">Returns the URL of the newly created item.</response>
        /// <response code="400">If the request is invalid or a department with the same title already exists.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentCreateRequestDto request)
        {
            if (await _context.Departments.AnyAsync(d => d.Title == request.Title))
            {
                return BadRequest($"Department with title '{request.Title}' already exists.");
            }

            var department = new Department { Title = request.Title };
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString();

            if (string.IsNullOrEmpty(apiVersion))
            {
                apiVersion = "1"; 
            }
            else if (!apiVersion.StartsWith("v")) 
            {
                apiVersion = $"v{apiVersion}";
            }

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            var locationUrl = $"{baseUrl}/api/{apiVersion}/departments/{department.Id}";

            return Created(locationUrl, null); 
        }

        /// <summary>
        /// Updates an existing department.
        /// </summary>
        /// <param name="id">The ID of the department to update.</param>
        /// <param name="request">Department update details.</param>
        /// <returns>No content on successful update.</returns>
        /// <response code="200">If the department was successfully updated.</response>
        /// <response code="400">If the request is invalid or a department with the updated title already exists.</response>
        /// <response code="404">If the department with the specified ID is not found.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentUpdateRequestDto request)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            if (await _context.Departments.AnyAsync(d => d.Title == request.Title && d.Id != id))
            {
                return BadRequest($"Another department with title '{request.Title}' already exists.");
            }

            department.Title = request.Title;
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Retrieves all departments.
        /// </summary>
        /// <returns>A list of all departments.</returns>
        /// <response code="200">Returns the list of departments.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DepartmentResponseDto>>> GetAllDepartments()
        {
            var departments = await _context.Departments
                .Select(d => new DepartmentResponseDto(d.Id, d.Title))
                .ToListAsync();
            return Ok(departments);
        }

        /// <summary>
        /// Retrieves a department by its ID.
        /// </summary>
        /// <param name="id">The ID of the department to retrieve.</param>
        /// <returns>The department details.</returns>
        /// <response code="200">Returns the department details.</response>
        /// <response code="404">If the department is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DepartmentResponseDto>> GetDepartmentById(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();
            return new DepartmentResponseDto(department.Id, department.Title);
        }
    }
}