using LoginPage.Server.Data;
using LoginPage.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginPage.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // ✅ Перевірка: чи прийшов null DTO
            if (dto == null)
            {
                return BadRequest("Недійсні дані.");
            }

            // ✅ Перевірка: чи введено пароль
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Пароль не може бути порожнім.");
            }

            // ✅ Перевірка: чи вже існує email
            if (await _context.SysUsers.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("Користувач з таким email вже існує.");
            }

            var newUser = new SysUser
            {
                TitleBeforeName = "",
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                TitleAfterName = "",
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
                CreatedAt = DateTime.Now,
                StatusEnumItemId = 1,
                Guid = Guid.NewGuid()
            };

            _context.SysUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("Користувача зареєстровано успішно.");
        }

        // 🔒 DTO можна залишити тут або винести в окремий файл
        public class RegisterDto
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
        }
    }
}
