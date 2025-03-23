using LoginPage.Server.Data;
using LoginPage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace LoginPage.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("Отримано запит на реєстрацію для email: {Email}", dto?.Email);

                // Валідація вхідних даних
                if (dto == null)
                {
                    _logger.LogWarning("Отримано null DTO при реєстрації");
                    return BadRequest("Недійсні дані.");
                }

                if (string.IsNullOrWhiteSpace(dto.FirstName))
                {
                    return BadRequest("Ім'я не може бути порожнім.");
                }

                if (string.IsNullOrWhiteSpace(dto.LastName))
                {
                    return BadRequest("Прізвище не може бути порожнім.");
                }

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest("Email не може бути порожнім.");
                }

                if (string.IsNullOrWhiteSpace(dto.Password))
                {
                    return BadRequest("Пароль не може бути порожнім.");
                }

                if (dto.Password.Length < 6)
                {
                    return BadRequest("Пароль має бути не менше 6 символів.");
                }

                // Перевірка на існуючий email
                var existingUser = await _context.SysUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Спроба реєстрації з існуючим email: {Email}", dto.Email);
                    return BadRequest("Користувач з таким email вже існує.");
                }

                // Створення нового користувача
                var newUser = new SysUser
                {
                    TitleBeforeName = "",
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    TitleAfterName = "",
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Email = dto.Email,
                    StatusEnumItemId = 1,
                    Guid = Guid.NewGuid()
                };

                _context.SysUsers.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Користувач успішно зареєстрований: {Email}", dto.Email);
                return Ok("Користувача зареєстровано успішно.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при реєстрації користувача");
                return StatusCode(500, "Помилка сервера при реєстрації. Спробуйте пізніше.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Отримано запит на логін для email: {Email}", dto?.Email);

                // Валідація вхідних даних
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                {
                    _logger.LogWarning("Спроба увійти з неповними даними");
                    return BadRequest("Недійсні дані для входу.");
                }
                var hashed = BCrypt.Net.BCrypt.HashPassword("123");
                Console.WriteLine(hashed);
                // Пошук користувача за email
                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);

                // Перевірка наявності користувача
                if (user == null)
                {
                    _logger.LogWarning("Спроба увійти з неіснуючим email: {Email}", dto.Email);
                    return Unauthorized("Невірний email або пароль.");
                }

                // Перевірка пароля
                bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
                if (!validPassword)
                {
                    _logger.LogWarning("Спроба увійти з невірним паролем для email: {Email}", dto.Email);
                    return Unauthorized("Невірний email або пароль.");
                }

                // Створення JWT токена
                var token = GenerateJwtToken(user);

                _logger.LogInformation("Успішний вхід користувача: {Email}", dto.Email);
                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при вході користувача");
                return StatusCode(500, "Помилка сервера при вході. Спробуйте пізніше.");
            }
        }

        private string GenerateJwtToken(SysUser user)
        {
            // Отримання ключа з конфігурації або використання запасного значення
            var secretKey = _configuration["JWT:SecretKey"] ?? "defaultSecretKey12345678901234567890";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Створення claims для токена
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id?.ToString() ?? "0"),
                new Claim(ClaimTypes.Name, $"{user.FirstName ?? ""} {user.LastName ?? ""}")
            };

            // Визначення додаткових параметрів токена
            var issuer = _configuration["JWT:Issuer"] ?? "defaultIssuer";
            var audience = _configuration["JWT:Audience"] ?? "defaultAudience";
            var expiry = DateTime.Now.AddDays(7); // Термін дії токена - 7 днів

            // Створення токена
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            // Перетворення токена в рядок
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // DTO для реєстрації
        public class RegisterDto
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        // DTO для логіну
        public class LoginDto
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }
    }
}