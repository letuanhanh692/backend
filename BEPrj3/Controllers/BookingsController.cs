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
    public class BookingsController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public BookingsController(BusBookingContext context)
        {
            _context = context;
        }

        // GET: api/Bookings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            return await _context.Bookings.ToListAsync();
        }

        // GET: api/Bookings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDto>> GetBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User) // Bổ sung User để tránh lỗi null
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                    .ThenInclude(b => b.BusType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Kiểm tra null trước khi truy cập thuộc tính
            var bookingResponse = new BookingResponseDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId, // Thêm dòng này
                ScheduleId = booking.ScheduleId, // Thêm dòng này
                Name = booking.User?.Name ?? "Unknown",
                Age = booking.Age,
                Phone = booking.User?.Phone ?? "Unknown",
                Email = booking.User?.Email ?? "Unknown",
                SeatNumber = booking.SeatNumber,
                BookingDate = booking.BookingDate ?? DateTime.MinValue,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,

                // Thông tin chuyến đi
                BusNumber = booking.Schedule?.Bus?.BusNumber ?? "N/A",
                BusType = booking.Schedule?.Bus?.BusType?.TypeName ?? "N/A",
                DepartTime = booking.Schedule?.DepartureTime ?? DateTime.MinValue,
                ArrivalTime = booking.Schedule?.ArrivalTime ?? DateTime.MinValue,
                StartingPlace = booking.Schedule?.Route?.StartingPlace ?? "N/A",
                DestinationPlace = booking.Schedule?.Route?.DestinationPlace ?? "N/A",
                Distance = (double)(booking.Schedule?.Route?.Distance ?? 0)
            };


            return Ok(bookingResponse);
        }

        // PUT: api/Bookings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBooking(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return BadRequest();
            }

            _context.Entry(booking).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id))
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

        // POST: api/Bookings
        [HttpPost]
        public async Task<ActionResult<BookingResponseDto>> PostBooking(BookingRequestDto bookingRequestDto)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .FirstOrDefaultAsync(s => s.Id == bookingRequestDto.ScheduleId);

            if (schedule == null)
            {
                return BadRequest("Chuyến đi không tồn tại.");
            }

            int bookedSeats = await _context.Bookings
                .Where(b => b.ScheduleId == bookingRequestDto.ScheduleId)
                .SumAsync(b => b.SeatNumber);

            int availableSeats = schedule.Bus.TotalSeats - bookedSeats;

            if (availableSeats < bookingRequestDto.SeatNumber)
            {
                return BadRequest("Không đủ ghế để đặt.");
            }

            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.RouteId == schedule.RouteId && pl.BusTypeId == schedule.Bus.BusTypeId);

            if (priceList == null)
            {
                return BadRequest("Không tìm thấy giá vé cho chuyến đi này.");
            }
            if (schedule.DepartureTime < DateTime.Now)
            {
                return BadRequest("Cannot book a trip that has already passed.");
            }

            decimal pricePerSeat = priceList.Price;
            if (bookingRequestDto.Age < 5) pricePerSeat = 0;
            else if (bookingRequestDto.Age >= 5 && bookingRequestDto.Age <= 12) pricePerSeat *= 0.5M;
            else if (bookingRequestDto.Age > 50) pricePerSeat *= 0.3M;

            decimal totalAmount = pricePerSeat * bookingRequestDto.SeatNumber;

            var booking = new Booking
            {
                UserId = bookingRequestDto.UserId,
                ScheduleId = bookingRequestDto.ScheduleId,
                SeatNumber = bookingRequestDto.SeatNumber,
                Age = bookingRequestDto.Age,
                BookingDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Booked"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Cập nhật số ghế còn lại trong lịch trình
            schedule.AvailableSeats -= bookingRequestDto.SeatNumber;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            var bookingResponse = new BookingResponseDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                ScheduleId = booking.ScheduleId,
                SeatNumber = booking.SeatNumber,
                Age = booking.Age,
                BookingDate = (DateTime)booking.BookingDate,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,

                Name = bookingRequestDto.Name,
                Phone = bookingRequestDto.Phone,
                Email = bookingRequestDto.Email,

                BusNumber = schedule.Bus.BusNumber,
                BusType = schedule.Bus.BusType.TypeName,
                DepartTime = schedule.DepartureTime,
                ArrivalTime = schedule.ArrivalTime,
                StartingPlace = schedule.Route.StartingPlace,
                DestinationPlace = schedule.Route.DestinationPlace,
                Distance = (double)schedule.Route.Distance
            };

            return CreatedAtAction("GetBooking", new { id = booking.Id }, bookingResponse);
        }

        // DELETE: api/Bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            // Cập nhật lại số ghế còn lại khi hủy vé
            var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == booking.ScheduleId);
            if (schedule != null)
            {
                schedule.AvailableSeats += booking.SeatNumber;
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}

   