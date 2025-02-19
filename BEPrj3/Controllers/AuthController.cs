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
        public IActionResult LoginStaff([FromBody] LoginUserModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { message = "Invalid credentials" });
            }

            // Kiểm tra thông tin đăng nhập trong DB
            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password && u.RoleId == 2); // Kiểm tra roleId là 2 (Staff)

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Trả về id của user
            return Ok(new { userId = user.Id });
        }
        [HttpGet("get-staff-routes")]
        public async Task<IActionResult> GetStaffRoutes([FromQuery] int userId)
        {
            // Kiểm tra nếu userId không hợp lệ
            if (userId <= 0)
            {
                return BadRequest(new { message = "Invalid User ID" });
            }

            try
            {
                // Lấy các chuyến xe mà nhân viên quản lý, bao gồm lịch trình và thông tin đặt vé
                var staffRoutes = await _context.StaffRoutes
                    .Where(sr => sr.StaffId == userId)
                    .Include(sr => sr.Route) // Liên kết với Route
                        .ThenInclude(r => r.Schedules) // Lấy các lịch trình của chuyến xe
                            .ThenInclude(s => s.Bookings) // Lấy các booking cho từng lịch trình
                                .ThenInclude(b => b.User) // Lấy thông tin người đặt vé
                    .ToListAsync(); // Sử dụng async để tối ưu hiệu suất

                if (staffRoutes == null || !staffRoutes.Any())
                {
                    return NotFound(new { message = "No routes found for this staff" });
                }

                // Trả về dữ liệu với cấu trúc chi tiết theo yêu cầu
                var result = staffRoutes.Select(sr => new
                {
                    routeId = sr.Route.Id,
                    startingPlace = sr.Route.StartingPlace,
                    destinationPlace = sr.Route.DestinationPlace,
                    distance = sr.Route.Distance,
                    schedules = sr.Route.Schedules.Select(s => new
                    {
                        scheduleId = s.Id,
                        departureTime = s.DepartureTime,
                        arrivalTime = s.ArrivalTime,
                        date = s.Date,
                        availableSeats = s.AvailableSeats,
                        price = s.Price,
                        bookings = s.Bookings.Select(b => new
                        {
                            bookingId = b.Id,
                            userName = b.Name,
                            userEmail = b.User.Email,
                            seatNumber = b.SeatNumber,
                            age = b.Age,
                            bookingDate = b.BookingDate,
                            status = b.Status,
                            totalAmount = b.TotalAmount
                        }).ToList()
                    }).ToList()
                }).ToList();

                return Ok(new { staffRoutes = result });
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có vấn đề trong quá trình truy vấn
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
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