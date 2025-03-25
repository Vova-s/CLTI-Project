using LoginPage.Server.Data;
using LoginPage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace LoginPage.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        #region Dependencies
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        // Constructor for dependency injection
        public AccountController(AppDbContext context, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        #endregion

        #region Registration Endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("Received registration request for email: {Email}", dto?.Email);

                // Validate input data
                if (dto == null)
                {
                    _logger.LogWarning("Null DTO received during registration");
                    return BadRequest("Invalid data.");
                }

                if (string.IsNullOrWhiteSpace(dto.FirstName))
                {
                    return BadRequest("First name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(dto.LastName))
                {
                    return BadRequest("Last name cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest("Email cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return BadRequest("Password cannot be empty.");
                }

                if (dto.Password.Length < 6)
                {
                    return BadRequest("Password must be at least 6 characters long.");
                }

                // Check for an existing user with the same email
                var existingUser = await _context.SysUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Attempted registration with existing email: {Email}", dto.Email);
                    return BadRequest("A user with this email already exists.");
                }

                // Create a new user instance and hash the password using BCrypt
                var newUser = new SysUser
                {
                    TitleBeforeName = "",
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    TitleAfterName = "",
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    CreatedAt = DateTime.Now,
                    Email = dto.Email,
                    Guid = Guid.NewGuid()
                };

                // Save the new user to the database
                _context.SysUsers.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered successfully: {Email}", dto.Email);
                return Ok("User registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return StatusCode(500, "Server error during registration. Please try again later.");
            }
        }
        #endregion

        #region Login Endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Received login request for email: {Email}", dto?.Email);

                // Validate input data
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                {
                    _logger.LogWarning("Login attempt with incomplete data");
                    return BadRequest("Invalid login data.");
                }

                // Debug line (for demonstration purposes; remove in production)
                var hashed = BCrypt.Net.BCrypt.HashPassword("123");
                Console.WriteLine(hashed);

                // Find the user by email
                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existing email: {Email}", dto.Email);
                    return Unauthorized("Incorrect email or password.");
                }

                // Verify the password using BCrypt
                bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
                if (!validPassword)
                {
                    _logger.LogWarning("Incorrect password attempt for email: {Email}", dto.Email);
                    return Unauthorized("Incorrect email or password.");
                }

                // Generate a JWT token for the authenticated user
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User logged in successfully: {Email}", dto.Email);
                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login");
                return StatusCode(500, "Server error during login. Please try again later.");
            }
        }
        #endregion

        #region JWT Token Generation
        private string GenerateJwtToken(SysUser user)
        {
            // Retrieve the secret key from configuration or use a default value
            var secretKey = _configuration["JWT:SecretKey"] ?? "defaultSecretKey12345678901234567890";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Define claims for the token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id?.ToString() ?? "0"),
                new Claim(ClaimTypes.Name, $"{user.FirstName ?? ""} {user.LastName ?? ""}")
            };

            // Configure token parameters
            var issuer = _configuration["JWT:Issuer"] ?? "defaultIssuer";
            var audience = _configuration["JWT:Audience"] ?? "defaultAudience";
            var expiry = DateTime.Now.AddDays(7); // Token validity: 7 days

            // Create and sign the JWT token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            // Convert the token to a string and return it
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion

        #region DTO Classes
        // Data Transfer Object for User Registration
        public class RegisterDto
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        // Data Transfer Object for User Login
        public class LoginDto
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }
        #endregion
    }
}
