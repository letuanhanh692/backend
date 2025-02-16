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

        //[HttpPost("register-staff")]
        //public async Task<IActionResult> RegisterStaff([FromBody] RegisterUserModel model)
        //{
        //    if (
        //        string.IsNullOrWhiteSpace(model.Password) ||
        //        string.IsNullOrWhiteSpace(model.Name) ||
        //        string.IsNullOrWhiteSpace(model.Phone) ||
        //        string.IsNullOrWhiteSpace(model.Email) ||
        //        string.IsNullOrWhiteSpace(model.Address) ||
        //        string.IsNullOrWhiteSpace(model.IdCard) ||
        //        model.DateOfBirth == null)
        //    {
        //        return BadRequest("All fields are required.");
        //    }

        //    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        //    if (existingUser != null)
        //    {
        //        return BadRequest("Email already exists.");
        //    }

        //    // Đặt RoleId mặc định là 3 (Customer)
        //    int defaultRoleId = 2;
        //    var roleExists = await _context.Roles.AnyAsync(r => r.Id == defaultRoleId);
        //    if (!roleExists)
        //    {
        //        return BadRequest("Default RoleId does not exist.");
        //    }

        //    var newUser = new User
        //    {

        //        Password = model.Password,
        //        Name = model.Name,
        //        Phone = model.Phone,
        //        Email = model.Email,
        //        Address = model.Address,
        //        IdCard = model.IdCard,
        //        DateOfBirth = model.DateOfBirth,
        //        RoleId = defaultRoleId,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _context.Users.Add(newUser);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "User registered successfully." });
        //}
        [HttpPost("login-staff")]
        public IActionResult Login(string email, string password)
        {
            // Bước 1: Xác thực tài khoản Staff
            var staff = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password && u.RoleId == 2);

            if (staff == null)
            {
                return Unauthorized("Email hoặc mật khẩu không hợp lệ.");
            }

            // Bước 2: Lấy tất cả Route mà Staff phụ trách
            var routes = _context.StaffRoutes
                                 .Where(sr => sr.StaffId == staff.Id)
                                 .Select(sr => sr.RouteId)
                                 .ToList();

            var result = _context.Routes
                .Where(r => routes.Contains(r.Id))
                .Select(route => new
                {
                    route.Id,
                    route.StartingPlace,
                    route.DestinationPlace,
                    route.PriceRoute,
                    route.Distance,
                    Schedules = _context.Schedules
                                        .Where(s => s.RouteId == route.Id)
                                        .Select(sch => new
                                        {
                                            sch.Id,
                                            sch.DepartureTime,
                                            sch.ArrivalTime,
                                            Bookings = _context.Bookings
                                                               .Where(b => b.ScheduleId == sch.Id)
                                                               .Select(b => new
                                                               {
                                                                   b.Id,
                                                                   b.TotalAmount,
                                                                   User = _context.Users
                                                                                  .Where(u => u.Id == b.UserId)
                                                                                  .Select(u => new
                                                                                  {
                                                                                      u.Id,
                                                                                      u.Name,
                                                                                      u.Email,
                                                                                      u.Phone
                                                                                  }).FirstOrDefault()
                                                               }).ToList()
                                        }).ToList()
                }).ToList();

            // Bước 5: Trả về đầy đủ thông tin
            return Ok(new
            {
                Staff = new
                {
                    staff.Id,
                    staff.Username,
                    staff.Name,
                    staff.Email,
                    staff.Phone,
                    staff.Address,
                    staff.IdCard,
                    staff.DateOfBirth,
                    staff.Avatar,
                    staff.RoleId
                },
                Routes = result
            });
        }

        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginUserModel model)
        {
            // Tìm kiếm người dùng có Email, Password và RoleId là Admin (1)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password && u.RoleId == 1);

            if (user == null)
            {
                return Unauthorized("Thông tin đăng nhập không hợp lệ hoặc bạn không phải là Admin!");
            }

            // Tạo danh sách các claims để lưu trữ trong Token
            var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId", user.Id.ToString()),  // Thêm userId vào token
            new Claim(ClaimTypes.Role, user.RoleId.ToString()) // Thêm RoleId vào token
        };

            // Khóa bí mật để ký JWT
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyHereYourSecretKeyHere"));

            // Tạo JWT Token
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            // Trả về Token cho người dùng
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

    }
}