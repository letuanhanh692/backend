using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models;
using BEPrj3.Models.DTO;
using BEPrj3.Models.Vnpay;
using BEPrj3.Services; // Import service VNPay

namespace BEPrj3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly BusBookingContext _context;
        private readonly IVnPayService _vnPayService;

        public BookingsController(BusBookingContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
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
                .Include(b => b.User)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                    .ThenInclude(b => b.BusType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            var bookingResponse = new BookingResponseDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                ScheduleId = booking.ScheduleId,
                Name = booking.User?.Name ?? "Unknown",
                Age = booking.Age,
                Phone = booking.User?.Phone ?? "Unknown",
                Email = booking.User?.Email ?? "Unknown",
                SeatNumber = booking.SeatNumber,
                BookingDate = booking.BookingDate ?? DateTime.MinValue,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
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

        // POST: api/Bookings
        [HttpPost]
        public async Task<ActionResult<BookingResponseDto>> PostBooking(BookingRequestDto bookingRequestDto)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b.BusType)
                .FirstOrDefaultAsync(s => s.Id == bookingRequestDto.ScheduleId);

            if (schedule == null) return BadRequest("Chuyến đi không tồn tại.");

            int bookedSeats = await _context.Bookings
                .Where(b => b.ScheduleId == bookingRequestDto.ScheduleId)
                .SumAsync(b => b.SeatNumber);

            int availableSeats = schedule.Bus.TotalSeats - bookedSeats;
            if (availableSeats < bookingRequestDto.SeatNumber)
                return BadRequest("Không đủ ghế để đặt.");

            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.RouteId == schedule.RouteId && pl.BusTypeId == schedule.Bus.BusTypeId);

            if (priceList == null) return BadRequest("Không tìm thấy giá vé.");

            if (schedule.DepartureTime < DateTime.Now)
                return BadRequest("Không thể đặt vé cho chuyến đi đã khởi hành.");

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
                Status = "Pending Payment"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            schedule.AvailableSeats -= bookingRequestDto.SeatNumber;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBooking", new { id = booking.Id }, new { bookingId = booking.Id });
        }

        // POST: api/Bookings/Payment
        [HttpPost("payment")]
        public async Task<ActionResult> PostPayment([FromBody] PaymentInformationModel model)
        {
            var booking = await _context.Bookings.FindAsync(model.BookingId);
            if (booking == null) return NotFound("Không tìm thấy đơn đặt vé.");

            model.TotalAmount = (double)booking.TotalAmount;
            model.SeatCount = booking.SeatNumber;
            model.OrderDescription = $"Thanh toán vé xe: {booking.Id}";
            model.OrderType = "bus_ticket";

            string paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Ok(new { PaymentUrl = paymentUrl });
        }

        // GET: api/Bookings/PaymentCallback
        [HttpGet("paymentCallback")]
        public async Task<ActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success)
            {
                var booking = await _context.Bookings.FindAsync(response.BookingId);
                if (booking == null) return NotFound("Không tìm thấy đơn đặt vé.");

                booking.Status = "Paid";
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Thanh toán thành công!", response });
            }

            return BadRequest(new { message = "Thanh toán thất bại!", response });
        }

        // DELETE: api/Bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

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
