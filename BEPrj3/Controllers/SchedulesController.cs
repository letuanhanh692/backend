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
    public class SchedulesController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public SchedulesController(BusBookingContext context)
        {
            _context = context;
        }

        // 📌 GET: api/Schedules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 4)
        {
            if (page == 0 && pageSize == 0)
            {
                var allSchedules = await _context.Schedules
                    .Include(s => s.Route)
                    .Include(s => s.Bus)
                    .ThenInclude(b => b.BusType)
                    .Include(s => s.Bookings)
                    .OrderByDescending(s => s.Id) 
                    
                    .Select(s => new
                    {
                        s.Id,
                        RouteId = s.Route.Id,
                        BusNumber = s.Bus.BusNumber,
                        BusType = s.Bus.BusType.TypeName,
                        TotalSeats = s.Bus.TotalSeats,
                        AvailableSeats = s.Bus.TotalSeats - s.Bookings.Sum(b => b.SeatNumber),
                        s.DepartureTime,
                        s.ArrivalTime,
                        s.Date,
                        // Tính giá từ PriceLists của Route
                        Price = s.Price,
                        ImageBus = s.Bus.ImageBus
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalSchedules = allSchedules.Count,
                    totalPages = 1, // Vì lấy tất cả nên chỉ có 1 trang
                    currentPage = 1,
                    pageSize = allSchedules.Count, // Tất cả bản ghi
                    schedules = allSchedules
                });
            }

            // Nếu không phải trường hợp lấy tất cả thì thực hiện phân trang
            var schedules = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .Include(s => s.Bookings)
                .OrderByDescending(s => s.Id) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    RouteId = s.Route.Id,
                    BusNumber = s.Bus.BusNumber,
                    BusType = s.Bus.BusType.TypeName,
                    TotalSeats = s.Bus.TotalSeats,
                    AvailableSeats = s.Bus.TotalSeats - s.Bookings.Sum(b => b.SeatNumber),
                    s.DepartureTime,
                    s.ArrivalTime,
                    s.Date,
                   
                    // Tính giá từ PriceLists của Route
                    Price = s.Route.PriceLists
                        .Where(p => p.BusTypeId == s.Bus.BusTypeId)
                        .Select(p => p.Price)
                        .FirstOrDefault(),
                    ImageBus = s.Bus.ImageBus
                })
                .ToListAsync();

            var totalSchedules = await _context.Schedules.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalSchedules / pageSize);

            var result = new
            {
                totalSchedules,
                totalPages,
                currentPage = page,
                pageSize,
                schedules
            };

            return Ok(result);
        }

        // GET: api/Schedules/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetScheduleDetails(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .Include(s => s.Bookings)
               
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }


            var scheduleDetails = new
            {
                schedule.Id,
                BusNumber = schedule.Bus.BusNumber,
                BusType = schedule.Bus.BusType.TypeName,
                TotalSeats = schedule.Bus.TotalSeats,
                AvailableSeats = schedule.Bus.TotalSeats - schedule.Bookings.Sum(b => b.SeatNumber),
                schedule.DepartureTime,
                schedule.ArrivalTime,

                schedule.Route.StartingPlace,
                schedule.Route.DestinationPlace,
                schedule.Route.Distance,
                schedule.Price,
                ImageBus = schedule.Bus.ImageBus
            };
            // push code demo checkout

            return Ok(scheduleDetails);
        }

        // PUT: api/Schedules/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSchedule(int id, [FromBody] ScheduleRequest scheduleRequest)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            var bus = await _context.Buses.FindAsync(scheduleRequest.BusId);
            var route = await _context.Routes.FindAsync(scheduleRequest.RouteId);

            if (bus == null)
            {
                return NotFound(new { message = "Bus not found." });
            }

            if (route == null)
            {
                return NotFound(new { message = "Route not found." });
            }

            if (scheduleRequest.ArrivalTime <= scheduleRequest.DepartureTime)
            {
                return BadRequest(new { message = "ArrivalTime must be later than DepartureTime." });
            }

            schedule.BusId = scheduleRequest.BusId;
            schedule.RouteId = scheduleRequest.RouteId;
            schedule.DepartureTime = scheduleRequest.DepartureTime;
            schedule.ArrivalTime = scheduleRequest.ArrivalTime;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScheduleExists(id))
                {
                    return NotFound(new { message = "Schedule not found during update." });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Schedule updated successfully." });
        }

        [HttpPost]
        public async Task<ActionResult<Schedule>> PostSchedule([FromBody] ScheduleRequest scheduleRequest)
        {
            // Kiểm tra BusId và RouteId hợp lệ
            var bus = await _context.Buses.FindAsync(scheduleRequest.BusId);
            var route = await _context.Routes.FindAsync(scheduleRequest.RouteId);

            if (bus == null)
            {
                return NotFound(new { message = "Bus not found." });
            }

            if (route == null)
            {
                return NotFound(new { message = "Route not found." });
            }

            // Kiểm tra thời gian hợp lệ
            if (scheduleRequest.ArrivalTime <= scheduleRequest.DepartureTime)
            {
                return BadRequest(new { message = "ArrivalTime must be later than DepartureTime." });
            }

            // Lấy giá cơ bản từ trường PriceRoute trong Route
            decimal basePrice = route.PriceRoute ?? 0; // Nếu null, mặc định là 0

            if (basePrice == 0)
            {
                return BadRequest(new { message = "No base price found for this route." });
            }

            // Xác định hệ số nhân theo loại xe (BusTypeId)
            decimal multiplier = bus.BusTypeId switch
            {
                1 => 1.0m,  // Hạng phổ thông
                2 => 1.1m,  // Hạng trung
                3 => 1.2m,  // Hạng cao cấp
                4 => 1.3m,  // Hạng VIP
                _ => 1.0m   // Mặc định nếu BusTypeId không hợp lệ
            };

            // Tính giá cuối cùng
            decimal finalPrice = basePrice * multiplier;

            var schedule = new Schedule
            {
                BusId = scheduleRequest.BusId,
                RouteId = scheduleRequest.RouteId,
                DepartureTime = scheduleRequest.DepartureTime,
                ArrivalTime = scheduleRequest.ArrivalTime,
                Date = DateOnly.FromDateTime(scheduleRequest.Date),
                AvailableSeats = bus.TotalSeats, 
                Price = finalPrice 
            };

            // Lưu vào cơ sở dữ liệu
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Trả về thông tin lịch trình sau khi tạo
            var response = new
            {
                schedule.Id,
                schedule.BusId,
                schedule.RouteId,
                schedule.DepartureTime,
                schedule.ArrivalTime,
                schedule.Date,
                schedule.AvailableSeats,
                Price = schedule.Price // Trả về giá đã lưu
            };

            return CreatedAtAction(nameof(GetScheduleDetails), new { id = schedule.Id }, response);
        }


        // DELETE: api/Schedules/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchSchedules(
    [FromQuery] string startingPlace,
    [FromQuery] string destinationPlace,
    [FromQuery] DateTime? departureDateTime)
        {
            if (string.IsNullOrEmpty(startingPlace) || string.IsNullOrEmpty(destinationPlace))
            {
                return BadRequest(new { message = "StartingPlace và DestinationPlace là bắt buộc." });
            }

            // Chuẩn hóa chuỗi (đặt ngoài LINQ)
            string NormalizeString(string input)
            {
                if (string.IsNullOrEmpty(input)) return "";
                var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
                var sb = new System.Text.StringBuilder();
                foreach (var c in normalized)
                {
                    if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        sb.Append(c);
                }
                return sb.ToString().ToLower().Replace(" ", "");
            }

            // Chuẩn hóa từ khóa tìm kiếm
            var normalizedStartingPlace = NormalizeString(startingPlace);
            var normalizedDestinationPlace = NormalizeString(destinationPlace);

            // Truy vấn dữ liệu từ DB trước (chưa áp dụng Normalize)
            var schedules = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .Include(s => s.Bookings)
                .ToListAsync(); // Lấy toàn bộ dữ liệu về bộ nhớ

            // Sau đó lọc dữ liệu trong bộ nhớ
            var filteredSchedules = schedules
                .Where(s =>
                    NormalizeString(s.Route.StartingPlace).Contains(normalizedStartingPlace) &&
                    NormalizeString(s.Route.DestinationPlace).Contains(normalizedDestinationPlace) &&
                    (!departureDateTime.HasValue || s.DepartureTime.Date == departureDateTime.Value.Date))
                .Select(s => new
                {
                    s.Id,
                    BusNumber = s.Bus.BusNumber,
                    BusType = s.Bus.BusType.TypeName,
                    TotalSeats = s.Bus.TotalSeats,
                    AvailableSeats = s.Bus.TotalSeats - s.Bookings.Sum(b => b.SeatNumber),
                    s.DepartureTime,
                    s.ArrivalTime,
                    s.Route.StartingPlace,
                    s.Route.DestinationPlace,
                    Price = s.Price,
                    ImageBus = s.Bus.ImageBus
                })
                .ToList();

            if (filteredSchedules.Count == 0)
            {
                return NotFound(new { message = "Không tìm thấy chuyến nào." });
            }

            return Ok(filteredSchedules);
        }

        // GET: api/Schedules/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<object>>> GetSchedulesForToday()
        {
            var today = DateTime.Now.Date; // Lấy ngày hiện tại (không có giờ phút giây)

            var schedules = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .Include(s => s.Bookings)
                .Where(s => s.DepartureTime.Date == today) // Kiểm tra xem ngày khởi hành có phải là hôm nay không
                .Select(s => new
                {
                    s.Id,
                    BusNumber = s.Bus.BusNumber,
                    BusType = s.Bus.BusType.TypeName,
                    TotalSeats = s.Bus.TotalSeats,
                    AvailableSeats = s.Bus.TotalSeats - s.Bookings.Sum(b => b.SeatNumber),
                    s.DepartureTime,
                    s.ArrivalTime,
                    s.Route.StartingPlace,
                    s.Route.DestinationPlace,
                    Price = s.Price,
                    ImageBus = s.Bus.ImageBus
                })
                .ToListAsync();

            if (schedules.Count == 0)
            {
                return NotFound(new { message = "No schedules available for today." });
            }

            return Ok(schedules);
        }

        // GET: api/Schedules/searchadmin
        [HttpGet("searchadmin")]
        public async Task<ActionResult<IEnumerable<object>>> SearchAdmin(
     [FromQuery] string searchQuery, // Thêm tham số searchQuery
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 4
 )
        {
            var query = _context.Schedules.AsQueryable();

            // Kiểm tra và áp dụng điều kiện tìm kiếm theo searchQuery
            if (!string.IsNullOrEmpty(searchQuery))
            {
                // Kiểm tra xem từ khóa có phải là số (để tìm kiếm theo RouteId hoặc TotalSeats)
                if (int.TryParse(searchQuery, out int searchNumber))
                {
                    query = query.Where(s =>
                        s.RouteId == searchNumber || // Tìm kiếm theo RouteId
                        s.Bus.TotalSeats == searchNumber // Tìm kiếm theo TotalSeats
                    );
                }
                else
                {
                    // Nếu không phải số, tìm kiếm theo thời gian (DepartureTime, ArrivalTime)
                    query = query.Where(s =>
                        s.DepartureTime.ToString().Contains(searchQuery) || // Tìm theo thời gian khởi hành
                        s.ArrivalTime.ToString().Contains(searchQuery) // Tìm theo thời gian đến
                    );
                }
            }

            var totalSchedules = await query.CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalSchedules / pageSize);

            var schedules = await query
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .Include(s => s.Bookings)
                .Skip((page - 1) * pageSize) // Bỏ qua các bản ghi đã có ở các trang trước
                .Take(pageSize) // Lấy số bản ghi theo pageSize
                .Select(s => new
                {
                    s.RouteId,
                    TotalSeats = s.Bus.TotalSeats,
                    AvailableSeats = s.Bus.TotalSeats - s.Bookings.Sum(b => b.SeatNumber),
                    s.DepartureTime,
                    s.ArrivalTime
                })
                .ToListAsync();

            if (schedules.Count == 0)
            {
                return NotFound(new { message = "No schedules found matching the criteria." });
            }

            // Trả về kết quả với thông tin phân trang
            var result = new
            {
                totalSchedules, // Tổng số bản ghi
                totalPages,     // Tổng số trang
                currentPage = page, // Trang hiện tại
                pageSize,       // Số bản ghi mỗi trang
                schedules       // Dữ liệu lịch trình
            };

            return Ok(result);
        }




    }
}
