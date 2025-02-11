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

        // GET: api/Schedules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSchedules()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bookings)
                .Select(s => new
                {
                    s.Id,
                    RouteId = s.Route.Id,
                    TotalSeats = s.Bus.TotalSeats,
                    AvailableSeats = s.Bus.TotalSeats - s.Bookings.Sum(b => b.SeatNumber),
                    s.DepartureTime,
                    s.ArrivalTime
                })
                .ToListAsync();

            return Ok(schedules);
        }

        // GET: api/Schedules/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetScheduleDetails(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType) // Join bảng BusType
                .Include(s => s.Bookings) // Join bảng Booking để tính số ghế đã đặt
                .Include(s => s.Route.PriceLists) // Join bảng PriceList để lấy giá
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            // Lấy giá của PriceList dựa trên BusTypeId
            var price = schedule.Route.PriceLists
                .Where(p => p.BusTypeId == schedule.Bus.BusTypeId)
                .Select(p => p.Price)
                .FirstOrDefault();

            var scheduleDetails = new
            {
                schedule.Id,
                BusNumber = schedule.Bus.BusNumber,
                BusType = schedule.Bus.BusType.TypeName, // Lấy tên hạng xe
                TotalSeats = schedule.Bus.TotalSeats, // Tổng số ghế
                AvailableSeats = schedule.Bus.TotalSeats - schedule.Bookings.Sum(b => b.SeatNumber), // Tính số ghế còn lại
                schedule.DepartureTime,
                schedule.ArrivalTime,
                schedule.Route.StartingPlace,
                schedule.Route.DestinationPlace,
                schedule.Route.Distance, // Cách nhau
                Price = price // Trả về giá
            };

            return Ok(scheduleDetails);
        }

        // PUT: api/Schedules/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSchedule(int id, Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return BadRequest();
            }

            _context.Entry(schedule).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScheduleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Schedules
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // POST: api/Schedules
        // POST: api/Schedules
        [HttpPost]
        public async Task<ActionResult<Schedule>> PostSchedule([FromBody] ScheduleRequest scheduleRequest)
        {
            // Kiểm tra BusId và RouteId hợp lệ
            var bus = await _context.Buses.FindAsync(scheduleRequest.BusId);
            var route = await _context.Routes.FindAsync(scheduleRequest.RouteId);

            if (bus == null)
            {
                return NotFound("Bus not found.");
            }

            if (route == null)
            {
                return NotFound("Route not found.");
            }

            // Kiểm tra thời gian hợp lệ
            if (scheduleRequest.ArrivalTime <= scheduleRequest.DepartureTime)
            {
                return BadRequest("ArrivalTime must be later than DepartureTime.");
            }

            // Tạo đối tượng Schedule mới từ dữ liệu yêu cầu
            var schedule = new Schedule
            {
                BusId = scheduleRequest.BusId,
                RouteId = scheduleRequest.RouteId,
                DepartureTime = scheduleRequest.DepartureTime,
                ArrivalTime = scheduleRequest.ArrivalTime,
                AvailableSeats = bus.TotalSeats // Gán AvailableSeats bằng sức chứa của Bus
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Trả về thông tin Schedule bao gồm AvailableSeats
            var response = new
            {
                schedule.Id,
                schedule.BusId,
                schedule.RouteId,
                schedule.DepartureTime,
                schedule.ArrivalTime,
                schedule.AvailableSeats
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
                return BadRequest(new { message = "StartingPlace and DestinationPlace are required." });
            }

            var schedules = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .Include(s => s.Bookings)
                .Include(s => s.Route.PriceLists) // Thêm PriceList vào
                .Where(s => s.Route.StartingPlace == startingPlace
                            && s.Route.DestinationPlace == destinationPlace
                            && (!departureDateTime.HasValue || s.DepartureTime.Date == departureDateTime.Value.Date))
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
                    Price = s.Route.PriceLists
                        .Where(p => p.BusTypeId == s.Bus.BusTypeId)
                        .Select(p => p.Price)
                        .FirstOrDefault() // Lấy giá phù hợp với BusTypeId
                })
                .ToListAsync();

            if (schedules.Count == 0)
            {
                return NotFound(new { message = "No schedules found for the given criteria." });
            }

            return Ok(schedules);
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
                    Price = s.Route.PriceLists
                        .Where(p => p.BusTypeId == s.Bus.BusTypeId)
                        .Select(p => p.Price)
                        .FirstOrDefault() // Lấy giá phù hợp với BusTypeId
                })
                .ToListAsync();

            if (schedules.Count == 0)
            {
                return NotFound(new { message = "No schedules available for today." });
            }

            return Ok(schedules);
        }




    }
}
