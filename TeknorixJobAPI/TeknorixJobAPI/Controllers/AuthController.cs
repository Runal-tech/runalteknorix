using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using Microsoft.AspNetCore.Authorization; 

namespace TeknorixJobAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")] 
    [Route("api/v{version:apiVersion}/[controller]")]
    
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="request">Login credentials (username and password).</param>
        /// <returns>A JWT token if authentication is successful.</returns>
        /// <response code="200">Returns the JWT token.</response>
        /// <response code="401">If authentication fails (invalid credentials).</response>
        [HttpPost("login")]
        [AllowAnonymous] 
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var configuredUsername = _configuration["AdminCredentials:Username"];
            var configuredPassword = _configuration["AdminCredentials:Password"];
          
            if (request.Username == configuredUsername && request.Password == configuredPassword)
            {
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, request.Username), 
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, "1"), 
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.Role, "Administrator"), 
                    new Claim(ClaimTypes.Role, "User") 
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expires = expires
                });
            }

            return Unauthorized("Invalid credentials.");
        }

        /// <summary>
        /// DTO for login request.
        /// </summary>
        public record LoginRequest(
            [FromForm(Name = "username")] string Username,
            [FromForm(Name = "password")] string Password
        );
    }
}