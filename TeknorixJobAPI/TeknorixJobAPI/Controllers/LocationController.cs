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
    public class LocationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public record LocationCreateRequestDto(
            [Required] string Title,
            [Required] string City,
            [Required] string State,
            [Required] string Country,
            [Required] string Zip
        );

        public record LocationUpdateRequestDto(
            [Required] string Title,
            [Required] string City,
            [Required] string State,
            [Required] string Country,
            [Required] string Zip
        );

        public record LocationResponseDto(
            int Id,
            string Title,
            string City,
            string State,
            string Country,
            string Zip
        );

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLocation([FromBody] LocationCreateRequestDto request)
        {
            var location = new Location
            {
                Title = request.Title,
                City = request.City,
                State = request.State,
                Country = request.Country,
                Zip = request.Zip
            };
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            var newLocationId = location.Id;
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

            var locationUrl = $"{baseUrl}/api/{apiVersion}/locations/{newLocationId}";

            return Created(locationUrl, null);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationUpdateRequestDto request)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            location.Title = request.Title;
            location.City = request.City;
            location.State = request.State;
            location.Country = request.Country;
            location.Zip = request.Zip;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetAllLocations()
        {
            var locations = await _context.Locations
                .Select(l => new LocationResponseDto(l.Id, l.Title, l.City, l.State, l.Country, l.Zip))
                .ToListAsync();
            return Ok(locations);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationResponseDto>> GetLocationById(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();
            return new LocationResponseDto(location.Id, location.Title, location.City, location.State, location.Country, location.Zip);
        }
    }
}