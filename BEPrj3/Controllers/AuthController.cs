using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BEPrj3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;




namespace BEPrj3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        //private readonly UserManager<ApplicationUser> _userManager;
        private readonly BusBookingContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(BusBookingContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //public AuthController(T2309mContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        //{
        //    _context = context;
        //    _userManager = userManager;
        //    _configuration = configuration;
        //}

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel model)
        {
            if (
                string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Phone) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Address) ||
                string.IsNullOrWhiteSpace(model.IdCard) ||
                model.DateOfBirth == null)
            {
                return BadRequest("All fields are required.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return BadRequest("Email already exists.");
            }

            // Đặt RoleId mặc định là 3 (Customer)
            int defaultRoleId = 3;
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == defaultRoleId);
            if (!roleExists)
            {
                return BadRequest("Default RoleId does not exist.");
            }

            var newUser = new User
            {
                
                Password = model.Password,
                Name = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                IdCard = model.IdCard,
                DateOfBirth = model.DateOfBirth, 
                RoleId = defaultRoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(a => a.Email == model.Email && a.Password == model.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("userId", user.Id.ToString()) // ✅ Thêm userId vào token
    };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHereYourSecretKeyHere"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        [HttpPost("register-staff")]
        public async Task<IActionResult> RegisterStaff([FromBody] RegisterUserModel model)
        {
            if (
                string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.Name) ||
                string.IsNullOrWhiteSpace(model.Phone) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Address) ||
                string.IsNullOrWhiteSpace(model.IdCard) ||
                model.DateOfBirth == null)
            {
                return BadRequest("All fields are required.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return BadRequest("Email already exists.");
            }

            // Đặt RoleId mặc định là 3 (Customer)
            int defaultRoleId = 2;
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == defaultRoleId);
            if (!roleExists)
            {
                return BadRequest("Default RoleId does not exist.");
            }

            var newUser = new User
            {

                Password = model.Password,
                Name = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                IdCard = model.IdCard,
                DateOfBirth = model.DateOfBirth,
                RoleId = defaultRoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }
        [HttpPost("login-staff")]
        public async Task<IActionResult> LoginStaff([FromBody] LoginUserModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(a => a.Email == model.Email && a.Password == model.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("userId", user.Id.ToString()) // ✅ Thêm userId vào token
    };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHereYourSecretKeyHere"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }
    }
}