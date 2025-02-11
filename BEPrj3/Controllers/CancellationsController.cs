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

        // GET: api/Cancellations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cancellation>>> GetCancellations()
        {
            return await _context.Cancellations.ToListAsync();
        }

        // GET: api/Cancellations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cancellation>> GetCancellation(int id)
        {
            var cancellation = await _context.Cancellations.FindAsync(id);

            if (cancellation == null)
            {
                return NotFound();
            }

            return cancellation;
        }

        // PUT: api/Cancellations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCancellation(int id, Cancellation cancellation)
        {
            if (id != cancellation.Id)
            {
                return BadRequest();
            }

            _context.Entry(cancellation).State = EntityState.Modified;

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

        // POST: api/Cancellations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // POST: api/Cancellations
        [HttpPost("{bookingId}")]
        public async Task<ActionResult<Cancellation>> PostCancellation(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var schedule = await _context.Schedules
                .Include(s => s.Bus) // Bao gồm thông tin xe
                .Include(s => s.Route) // Bao gồm thông tin tuyến đường
                .FirstOrDefaultAsync(s => s.Id == booking.ScheduleId);

            if (schedule == null)
            {
                return NotFound("Schedule not found.");
            }

            // Tính toán số tiền hoàn trả trực tiếp trong controller
            decimal refundAmount = 0;
            var timeToDeparture = schedule.DepartureTime - DateTime.Now;

            // Quy định hoàn trả theo thời gian
            if (timeToDeparture.TotalHours >= 24)
            {
                refundAmount = booking.TotalAmount;  // Hoàn 100% nếu hủy trước 24h
            }
            else if (timeToDeparture.TotalHours >= 0)
            {
                refundAmount = booking.TotalAmount * 0.5m;  // Hoàn 50% nếu hủy trong vòng 24h
            }
            else
            {
                refundAmount = 0;  // Không hoàn tiền nếu đã qua thời gian khởi hành
            }

            // Cập nhật trạng thái vé trong bảng Booking
            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);

            // Tạo bản ghi hủy vé nếu cần lưu lại lịch sử hủy vé
            var cancellation = new Cancellation
            {
                BookingId = booking.Id,
                CancellationDate = DateTime.Now,
                RefundAmount = refundAmount,
                // Không cần thêm Status vào Cancellation nữa
            };

            _context.Cancellations.Add(cancellation);
            await _context.SaveChangesAsync();

            // Thực hiện hoàn tiền (ví dụ qua VNPay hoặc phương thức thanh toán khác)
            bool refundSuccess = await ProcessRefund(booking, refundAmount);

            if (refundSuccess)
            {
                // Cập nhật lại số ghế còn lại trong lịch trình
                schedule.AvailableSeats += booking.SeatNumber; // Cộng lại số ghế đã hủy
                _context.Schedules.Update(schedule); // Cập nhật lại thông tin lịch trình
                await _context.SaveChangesAsync();
            }

            return Ok(cancellation);
        }

        private async Task<bool> ProcessRefund(Booking booking, decimal refundAmount)
        {
            try
            {
                // Giả lập xử lý hoàn tiền
                Console.WriteLine($"Refund processed for Booking Id: {booking.Id} with Amount: {refundAmount}");

                // Ở đây bạn có thể gọi đến cổng thanh toán như VNPay hoặc cập nhật trạng thái thanh toán của bạn.

                return true; // Giả sử hoàn tiền thành công
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có vấn đề xảy ra
                Console.WriteLine($"Error processing refund: {ex.Message}");
                return false; // Hoàn tiền thất bại
            }
        }


        // DELETE: api/Cancellations/5
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
