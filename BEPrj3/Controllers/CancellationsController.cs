using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models;

namespace BEPrj3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancellationsController : ControllerBase
    {
        private readonly BusBookingContext _context;

        public CancellationsController(BusBookingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cancellation>>> GetCancellations(int page = 1, int pageSize = 7)
        {
            if (page == 0 && pageSize == 0)
            {
                var allCancellations = await _context.Cancellations
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                return Ok(new
                {
                    Cancellations = allCancellations,
                    TotalPages = 1,
                    CurrentPage = 1
                });
            }

            var totalCount = await _context.Cancellations.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var cancellations = await _context.Cancellations
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Cancellations = cancellations,
                TotalPages = totalPages,
                CurrentPage = page
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCancellationDetail(int id)
        {
            var cancellationDetail = await _context.Cancellations
                .Include(c => c.Booking)
                    .ThenInclude(b => b.User)
                .Include(c => c.Booking.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(c => c.Booking.Schedule.Bus)
                    .ThenInclude(b => b.BusType)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    CancellationId = c.Id,
                    CancellationDate = c.CancellationDate,
                    RefundAmount = c.RefundAmount,
                    BookingId = c.BookingId,
                    Name = c.Booking.Name,
                    Age = c.Booking.Age,
                    Phone = c.Booking.User.Phone,
                    Email = c.Booking.User.Email,
                    SeatNumber = c.Booking.SeatNumber,
                    BookingDate = c.Booking.BookingDate,
                    TotalAmount = c.Booking.TotalAmount,
                    Status = c.Booking.Status,
                    BusNumber = c.Booking.Schedule.Bus.BusNumber,
                    BusType = c.Booking.Schedule.Bus.BusType.TypeName,
                    DepartTime = c.Booking.Schedule.DepartureTime,
                    ArrivalTime = c.Booking.Schedule.ArrivalTime,
                    StartingPlace = c.Booking.Schedule.Route.StartingPlace,
                    DestinationPlace = c.Booking.Schedule.Route.DestinationPlace,
                    Distance = c.Booking.Schedule.Route.Distance,
                    UserId = c.Booking.User.Id,
                    ScheduleId = c.Booking.ScheduleId
                })
                .FirstOrDefaultAsync();

            if (cancellationDetail == null)
            {
                return NotFound(new { Message = "No details found for the given Cancellation ID." });
            }

            return Ok(cancellationDetail);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCancellation(int id, [FromBody] Cancellation updatedCancellation)
        {
            if (id != updatedCancellation.Id)
            {
                return BadRequest();
            }

            var existingCancellation = await _context.Cancellations.FindAsync(id);
            if (existingCancellation == null)
            {
                return NotFound();
            }

            existingCancellation.BookingId = updatedCancellation.BookingId;
            existingCancellation.CancellationDate = updatedCancellation.CancellationDate;
            existingCancellation.RefundAmount = updatedCancellation.RefundAmount;

            var booking = await _context.Bookings.FindAsync(updatedCancellation.BookingId);
            if (booking == null)
            {
                return BadRequest(new { Message = "Booking không tồn tại." });
            }
            existingCancellation.Booking = booking;

            _context.Entry(existingCancellation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CancellationExists(id))
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

        [HttpPost("{bookingId}")]
        public async Task<ActionResult<Cancellation>> PostCancellation(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // ✅ Include Bus để lấy TotalSeats
            var schedule = await _context.Schedules
                .Include(s => s.Bus) // Load Bus để lấy tổng ghế
                .FirstOrDefaultAsync(s => s.Id == booking.ScheduleId);

            if (schedule == null)
            {
                return NotFound("Schedule not found.");
            }

            if (schedule.Bus == null)
            {
                return BadRequest("Bus information is missing for this schedule.");
            }

            int totalSeats = schedule.Bus.TotalSeats; // ✅ Lấy TotalSeats từ Bus

            // ✅ Kiểm tra số ghế đặt
            int bookedSeats = booking.SeatNumber; // Nếu SeatNumber là số ghế đã đặt
            if (bookedSeats <= 0)
            {
                return BadRequest("Invalid number of booked seats.");
            }

            // ✅ Tính số tiền hoàn lại
            decimal refundAmount = 0;
            var timeToDeparture = schedule.DepartureTime - DateTime.Now;

            if (timeToDeparture.TotalHours >= 24)
            {
                refundAmount = booking.TotalAmount; // Hoàn 100% nếu huỷ trước 24h
            }
            else if (timeToDeparture.TotalHours >= 0)
            {
                refundAmount = booking.TotalAmount * 0.5m; // Hoàn 50% nếu huỷ trong vòng 24h
            }
            else
            {
                refundAmount = 0; // Không hoàn nếu đã qua giờ khởi hành
            }

            // ✅ Cập nhật trạng thái Booking
            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);

            // ✅ Tạo bản ghi Cancellation
            var cancellation = new Cancellation
            {
                BookingId = booking.Id,
                CancellationDate = DateTime.Now,
                RefundAmount = refundAmount,
            };
            _context.Cancellations.Add(cancellation);

            // ✅ Cộng lại đúng số ghế đã hủy
            schedule.AvailableSeats += bookedSeats;
            if (schedule.AvailableSeats > totalSeats)
            {
                schedule.AvailableSeats = totalSeats; // Không vượt quá tổng ghế xe buýt
            }
            _context.Schedules.Update(schedule);

            await _context.SaveChangesAsync();

            // ✅ Giả lập hoàn tiền
            bool refundSuccess = await ProcessRefund(booking, refundAmount);
            if (!refundSuccess)
            {
                return StatusCode(500, "Refund failed.");
            }

            return Ok(new
            {
                CancellationId = cancellation.Id,
                BookingId = booking.Id,
                RefundAmount = refundAmount,
                CancellationDate = cancellation.CancellationDate,
                AvailableSeats = schedule.AvailableSeats
            });
        }

        private async Task<bool> ProcessRefund(Booking booking, decimal refundAmount)
        {
            try
            {
                // 🔔 Giả lập logic hoàn tiền
                Console.WriteLine($"Refund processed for Booking Id: {booking.Id} with Amount: {refundAmount}");

                // Nếu tích hợp VNPay hoặc cổng thanh toán thực tế:
                // - Gọi API hoàn tiền tại đây
                // - Kiểm tra kết quả và trả về true/false

                await Task.Delay(100); // Giả lập thời gian xử lý

                return true; // Trả về true nếu hoàn tiền thành công
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing refund: {ex.Message}");
                return false; // Trả về false nếu hoàn tiền thất bại
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCancellation(int id)
        {
            var cancellation = await _context.Cancellations.FindAsync(id);
            if (cancellation == null)
            {
                return NotFound();
            }

            _context.Cancellations.Remove(cancellation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CancellationExists(int id)
        {
            return _context.Cancellations.Any(e => e.Id == id);
        }
    }
}
