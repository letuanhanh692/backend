    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using BEPrj3.Models;
    using BEPrj3.Models.DTO;

    namespace BEPrj3.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class UserDTOController : ControllerBase
        {
            private readonly BusBookingContext _context;

            public UserDTOController(BusBookingContext context)
            {
                _context = context;
            }

            // GET: api/UserDTO
            [HttpGet]
            public async Task<ActionResult<IEnumerable<UserDTO>>> GetUserDTO(int page = 1, int limit = 4)
            {
                var usersWithRoles = await _context.Users
                    .Include(u => u.Role)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Password = u.Password,
                        Name = u.Name,
                        Email = u.Email,
                        Phone = u.Phone,
                        Address = u.Address,
                        IdCard = u.IdCard,
                        DateOfBirth = u.DateOfBirth,
                        Avatar = u.Avatar,
                        RoleId = u.Role.Id,
                        RoleName = u.Role.Name,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(usersWithRoles);
            }

            // GET: api/UserDTO/5
            [HttpGet("{id}")]
            public async Task<ActionResult<UserDTO>> GetUserDTO(int id)
            {
                var userWithRole = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Id == id)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Password = u.Password,
                        Name = u.Name,
                        Email = u.Email,
                        Phone = u.Phone,
                        Address = u.Address,
                        IdCard = u.IdCard,
                        DateOfBirth = u.DateOfBirth,
                        Avatar = u.Avatar,
                        RoleId = u.Role.Id,
                        RoleName = u.Role.Name,
                        CreatedAt = u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (userWithRole == null)
                {
                    return NotFound();
                }

                return Ok(userWithRole);
            }

            // PUT: api/UserDTO/5
            [HttpPut("{id}")]
            public async Task<IActionResult> PutUserDTO(int id, UserDTO userDTO)
            {
                if (id != userDTO.Id)
                {
                    return BadRequest();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var role = await _context.Roles.FindAsync(userDTO.RoleId);
                if (role == null)
                {
                    return BadRequest("Role không tồn tại.");
                }

                user.Username = userDTO.Username;
                user.Password = userDTO.Password;
                user.Name = userDTO.Name;
                user.Email = userDTO.Email;
                user.Phone = userDTO.Phone;
                user.Address = userDTO.Address;
                user.IdCard = userDTO.IdCard;
                user.DateOfBirth = userDTO.DateOfBirth;
                user.Avatar = userDTO.Avatar;
                user.RoleId = userDTO.RoleId;
                user.CreatedAt = userDTO.CreatedAt;

                _context.Entry(user).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserDTOExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Gán roleName từ role
                userDTO.RoleName = role.Name;

                return NoContent();
            }

            // POST: api/UserDTO
            [HttpPost]
            public async Task<ActionResult<UserDTO>> PostUserDTO(UserDTO userDTO)
            {
                // Kiểm tra email trùng
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userDTO.Email);

                if (existingUserByEmail != null)
                {
                    return BadRequest("Email đã tồn tại.");
                }

                var existingUserByIdCard = await _context.Users
                    .FirstOrDefaultAsync(u => u.IdCard == userDTO.IdCard);

                if (existingUserByIdCard != null)
                {
                    return BadRequest("ID Card đã tồn tại.");
                }

                // Kiểm tra role tồn tại
                var role = await _context.Roles.FindAsync(userDTO.RoleId);
                if (role == null)
                {
                    return BadRequest("Role không tồn tại.");
                }

                // Tạo entity người dùng mới
                var userEntity = new User
                {
                    Username = userDTO.Username,
                    Password = userDTO.Password,
                    Name = userDTO.Name,
                    Email = userDTO.Email,
                    Phone = userDTO.Phone,
                    Address = userDTO.Address,
                    IdCard = userDTO.IdCard,
                    DateOfBirth = userDTO.DateOfBirth,
                    Avatar = userDTO.Avatar,
                    RoleId = userDTO.RoleId,
                    CreatedAt = DateTime.Now
                };

                // Thêm người dùng mới vào cơ sở dữ liệu
                _context.Users.Add(userEntity);
                await _context.SaveChangesAsync();

                // Gán roleName từ role
                userDTO.Id = userEntity.Id;
                userDTO.RoleName = role.Name;

                return CreatedAtAction("GetUserDTO", new { id = userDTO.Id }, userDTO);
            }

            // DELETE: api/UserDTO/5
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteUserDTO(int id)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            private bool UserDTOExists(int id)
            {
                return _context.Users.Any(e => e.Id == id);
            }

            // Tìm kiếm Users theo tên, điện thoại hoặc địa chỉ
            [HttpGet("search")]
            public async Task<ActionResult<IEnumerable<UserDTO>>> SearchUsers(string query, int page = 1, int limit = 4)
            {
                var totalUsers = await _context.Users
                    .Where(u => u.Username.Contains(query) || u.Phone.Contains(query) || u.Address.Contains(query))
                    .CountAsync();

                var totalPages = (int)Math.Ceiling((double)totalUsers / limit);

                var users = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Username.Contains(query) || u.Phone.Contains(query) || u.Address.Contains(query))
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Name = u.Name,
                        Email = u.Email,
                        Phone = u.Phone,
                        Address = u.Address,
                        RoleName = u.Role.Name,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { users, totalPages });
            }
        }
    }
