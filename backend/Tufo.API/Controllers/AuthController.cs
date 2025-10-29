using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tufo.Infrastructure.Identity;

namespace Tufo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // allow register/login without a token
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _users;
        private readonly IConfiguration _cfg;

        public AuthController(UserManager<AppUser> users, IConfiguration cfg)
        {
            _users = users;
            _cfg = cfg;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match");

            // username unique
            if (await _users.FindByNameAsync(dto.UserName) is not null)
                return BadRequest("Username already exists");

            // optional: enforce unique email here if you didn’t set it in Identity options
            var existingByEmail = await _users.FindByEmailAsync(dto.Email);
            if (existingByEmail is not null)
                return BadRequest("Email already in use");

            var u = new AppUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                DisplayName = dto.UserName
            };

            var res = await _users.CreateAsync(u, dto.Password);
            if (!res.Succeeded) return BadRequest(res.Errors);

            return Ok(new { token = MakeJwt(u) });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return Unauthorized("Empty email or password");

            var u = await _users.FindByEmailAsync(dto.Email);
            if (u is null) return Unauthorized("User not found");

            if (!await _users.CheckPasswordAsync(u, dto.Password))
                return Unauthorized("Invalid password");

            return Ok(new { token = MakeJwt(u) });
        }

        private string MakeJwt(AppUser u)
        {
            var jwt = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, u.Id),
                new Claim(ClaimTypes.NameIdentifier, u.Id),
                new Claim(JwtRegisteredClaimNames.Email, u.Email ?? ""),
                new Claim("name", u.DisplayName ?? u.Email ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // KEEP ONLY THESE DTOs
        public record RegisterDto(string UserName, string Email, string Password, string ConfirmPassword);
        public record LoginDto(string Email, string Password);
    }
}