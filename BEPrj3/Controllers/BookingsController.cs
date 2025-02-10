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
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            return booking;
        }

        // PUT: api/Bookings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Booking>> PostBooking(BookingRequestDto bookingRequestDto)
        {
            // Tìm lịch trình chuyến đi từ ID
            var schedule = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.Id == bookingRequestDto.ScheduleId);

            if (schedule == null)
            {
                return BadRequest("Chuyến đi không tồn tại.");
            }

            // Tính tổng số ghế đã đặt cho chuyến đi này
            int bookedSeats = await _context.Bookings
                .Where(b => b.ScheduleId == bookingRequestDto.ScheduleId)
                .SumAsync(b => b.SeatNumber);

            // Tính số ghế còn lại
            int availableSeats = schedule.Bus.TotalSeats - bookedSeats;

            // Kiểm tra số ghế có sẵn
            if (availableSeats < bookingRequestDto.SeatNumber)
            {
                return BadRequest("Không đủ ghế để đặt.");
            }


            // Tính giá vé từ PriceList
            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.RouteId == schedule.RouteId && pl.BusTypeId == schedule.Bus.BusTypeId);

            if (priceList == null)
            {
                return BadRequest("Không tìm thấy giá vé cho chuyến đi này.");
            }

            // Tính tổng tiền
            decimal totalAmount = 0;
            decimal pricePerSeat = priceList.Price;

            // Xử lý logic tính tiền theo độ tuổi
            if (bookingRequestDto.Age < 5)
            {
                pricePerSeat = 0;  // Miễn phí
            }
            else if (bookingRequestDto.Age >= 5 && bookingRequestDto.Age <= 12)
            {
                pricePerSeat *= 0.5M;  // Giảm 50% cho khách từ 5 đến 12 tuổi
            }
            else if (bookingRequestDto.Age > 50)
            {
                pricePerSeat *= 0.3M;  // Giảm 70% cho khách trên 50 tuổi
            }

            totalAmount = pricePerSeat * bookingRequestDto.SeatNumber;  // Tổng tiền cho số lượng ghế

            // Tạo booking
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

            // Tạo chi tiết booking (mỗi khách hàng trong booking)
            for (int i = 0; i < bookingRequestDto.SeatNumber; i++)
            {
                var bookingDetail = new BookingDetailDto
                {
                    BookingId = booking.Id,
                    Name = bookingRequestDto.Name,
                    Age = bookingRequestDto.Age,
                    Phone = bookingRequestDto.Phone,
                    Email = bookingRequestDto.Email,
                    SeatNumber = i + 1  // Giả sử ghế bắt đầu từ 1
                };

                // Bạn có thể thêm logic lưu chi tiết vé vào bảng nếu cần
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBooking", new { id = booking.Id }, booking);
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

            return NoContent();
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}
