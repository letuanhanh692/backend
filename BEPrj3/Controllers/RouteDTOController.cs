using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BEPrj3.Models.DTO;
using Route = BEPrj3.Models.Route;

namespace BEPrj3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public RouteController(BusBookingContext context)
        {
            _context = context;
        }

        // GET: api/Route
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteDTO>>> GetRoutes(int page = 1, int pageSize = 4)
        {
            // Tính tổng số bản ghi trong cơ sở dữ liệu
            var totalItems = await _context.Routes.CountAsync();

            // Nếu page = 0 và pageSize = 0 thì lấy tất cả bản ghi
            if (page == 0 && pageSize == 0)
            {
                var allRoutes = await _context.Routes
                    .Include(r => r.StaffRoutes)
                    .ThenInclude(sr => sr.Staff)
                    .ToListAsync();

                var allRouteDTOs = allRoutes.Select(route => new RouteDTO
                {
                    Id = route.Id,
                    StartingPlace = route.StartingPlace,
                    DestinationPlace = route.DestinationPlace,
                    Distance = route.Distance,
                    PriceRoute = route.PriceRoute ?? 0,
                    StaffId = route.StaffRoutes.FirstOrDefault()?.StaffId ?? 0,
                    StaffName = route.StaffRoutes.FirstOrDefault()?.Staff.Name ?? "N/A",
                    StaffEmail = route.StaffRoutes.FirstOrDefault()?.Staff.Email ?? "N/A"
                }).ToList();

                var resultAll = new
                {
                    TotalItems = totalItems,
                    CurrentPage = 1,
                    Routes = allRouteDTOs
                };

                return Ok(resultAll);
            }

            // Chỉ lấy dữ liệu của trang hiện tại nếu không phải lấy tất cả
            var skip = (page - 1) * pageSize;

            var routes = await _context.Routes
                .Include(r => r.StaffRoutes)
                .ThenInclude(sr => sr.Staff)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var routeDTOs = routes.Select(route => new RouteDTO
            {
                Id = route.Id,
                StartingPlace = route.StartingPlace,
                DestinationPlace = route.DestinationPlace,
                Distance = route.Distance,
                PriceRoute = route.PriceRoute ?? 0,
                StaffId = route.StaffRoutes.FirstOrDefault()?.StaffId ?? 0,
                StaffName = route.StaffRoutes.FirstOrDefault()?.Staff.Name ?? "N/A",
                StaffEmail = route.StaffRoutes.FirstOrDefault()?.Staff.Email ?? "N/A"
            }).ToList();

            var result = new
            {
                TotalItems = totalItems,  // Tổng số bản ghi
                CurrentPage = page,
                Routes = routeDTOs
            };

            return Ok(result);
        }



        // GET: api/Route/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteDTO>> GetRoute(int id)
        {
            var route = await _context.Routes
                .Include(r => r.StaffRoutes)
                    .ThenInclude(sr => sr.Staff)  // Lấy thông tin nhân viên
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null)
            {
                return NotFound();
            }

            // Ánh xạ từ Route sang RouteDTO
            var routeDTO = new RouteDTO
            {
                Id = route.Id,
                StartingPlace = route.StartingPlace,
                DestinationPlace = route.DestinationPlace,
                Distance = route.Distance,
                PriceRoute = route.PriceRoute ?? 0, // Nếu PriceRoute là null, đặt giá trị mặc định là 0
                // Lấy thông tin nhân viên đầu tiên liên quan đến tuyến đường (nếu có)
                StaffId = route.StaffRoutes.FirstOrDefault()?.StaffId ?? 0,
                StaffName = route.StaffRoutes.FirstOrDefault()?.Staff.Name ?? "N/A",
                StaffEmail = route.StaffRoutes.FirstOrDefault()?.Staff.Email ?? "N/A"
            };

            return Ok(routeDTO);
        }

        // POST: api/Route
        [HttpPost]
        public async Task<ActionResult<RouteDTO>> PostRoute(RouteDTO routeDTO)
        {
            // Chuyển RouteDTO sang đối tượng Route
            var route = new Route
            {
                StartingPlace = routeDTO.StartingPlace,
                DestinationPlace = routeDTO.DestinationPlace,
                Distance = routeDTO.Distance,
                PriceRoute = routeDTO.PriceRoute
            };

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            // Thêm mối quan hệ nhân viên vào StaffRoute
            if (routeDTO.StaffId > 0)
            {
                _context.StaffRoutes.Add(new StaffRoute
                {
                    StaffId = routeDTO.StaffId,
                    RouteId = route.Id
                });
            }

            await _context.SaveChangesAsync();

            // Trả về RouteDTO đã được tạo
            return CreatedAtAction("GetRoute", new { id = route.Id }, new RouteDTO
            {
                Id = route.Id,
                StartingPlace = route.StartingPlace,
                DestinationPlace = route.DestinationPlace,
                Distance = route.Distance,
                PriceRoute = route.PriceRoute ?? 0,
                StaffId = routeDTO.StaffId,
                StaffName = routeDTO.StaffName,
                StaffEmail = routeDTO.StaffEmail
            });
        }

        // PUT: api/Route/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoute(int id, RouteDTO routeDTO)
        {
            if (id != routeDTO.Id)
            {
                return BadRequest();
            }

            var existingRoute = await _context.Routes.FindAsync(id);
            if (existingRoute == null)
            {
                return NotFound();
            }

            existingRoute.StartingPlace = routeDTO.StartingPlace;
            existingRoute.DestinationPlace = routeDTO.DestinationPlace;
            existingRoute.Distance = routeDTO.Distance;
            existingRoute.PriceRoute = routeDTO.PriceRoute;

            // Cập nhật các mối quan hệ nhân viên
            var existingStaffRoutes = _context.StaffRoutes.Where(sr => sr.RouteId == id).ToList();
            _context.StaffRoutes.RemoveRange(existingStaffRoutes);

            if (routeDTO.StaffId > 0)
            {
                _context.StaffRoutes.Add(new StaffRoute
                {
                    StaffId = routeDTO.StaffId,
                    RouteId = id
                });
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Route/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            // Tìm tuyến đường trong bảng Routes
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound(); // Nếu không tìm thấy, trả về lỗi 404
            }

            // Xóa mối quan hệ nhân viên (StaffRoutes)
            var staffRoutes = _context.StaffRoutes.Where(sr => sr.RouteId == id).ToList();
            _context.StaffRoutes.RemoveRange(staffRoutes);

            // Xóa các lịch trình (Schedules) liên quan đến tuyến đường
            var schedules = _context.Schedules.Where(s => s.RouteId == id).ToList();
            _context.Schedules.RemoveRange(schedules);


            // Cuối cùng, xóa tuyến đường
            _context.Routes.Remove(route);

            // Lưu các thay đổi vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            return NoContent(); // Trả về mã trạng thái 204 khi xóa thành công
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<RouteDTO>>> SearchRoutes(int page = 1, int pageSize = 4, string searchQuery = "")
        {
            // Tính tổng số bản ghi trong cơ sở dữ liệu có chứa searchQuery trong cả StartingPlace và DestinationPlace
            var totalItems = await _context.Routes
                .Where(r => r.StartingPlace.Contains(searchQuery) || r.DestinationPlace.Contains(searchQuery))
                .CountAsync();

            // Chỉ lấy dữ liệu của trang hiện tại
            var skip = (page - 1) * pageSize;

            // Lấy danh sách tuyến đường, chỉ lấy 4 tuyến đường mỗi trang và lọc theo searchQuery
            var routes = await _context.Routes
                .Where(r => r.StartingPlace.Contains(searchQuery) || r.DestinationPlace.Contains(searchQuery))
                .Include(r => r.StaffRoutes)
                .ThenInclude(sr => sr.Staff)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // Ánh xạ dữ liệu từ Route sang RouteDTO
            var routeDTOs = routes.Select(route => new RouteDTO
            {
                Id = route.Id,
                StartingPlace = route.StartingPlace,
                DestinationPlace = route.DestinationPlace,
                Distance = route.Distance,
                PriceRoute = route.PriceRoute ?? 0,
                StaffId = route.StaffRoutes.FirstOrDefault()?.StaffId ?? 0,
                StaffName = route.StaffRoutes.FirstOrDefault()?.Staff.Name ?? "N/A",
                StaffEmail = route.StaffRoutes.FirstOrDefault()?.Staff.Email ?? "N/A"
            }).ToList();

            // Trả về kết quả với dữ liệu của trang hiện tại và tổng số bản ghi
            var result = new
            {
                TotalItems = totalItems,  // Tổng số bản ghi
                CurrentPage = page,
                Routes = routeDTOs
            };

            return Ok(result);
        }

    }
}
