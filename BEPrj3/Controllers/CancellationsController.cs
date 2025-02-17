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
        public async Task<ActionResult<IEnumerable<Cancellation>>> GetCancellations(int page = 1, int pageSize = 7)
        {
            if (page == 0 && pageSize == 0)
            {
                var allCancellations = await _context.Cancellations
                    .OrderByDescending(c => c.Id) // Sắp xếp mới nhất lên trước
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
                .OrderByDescending(c => c.Id)  // Sắp xếp mới nhất lên trước
                .Skip((page - 1) * pageSize)   // Bỏ qua các bản ghi trước trang hiện tại
                .Take(pageSize)                // Lấy giới hạn số bản ghi cho trang hiện tại
                .ToListAsync();

            return Ok(new
            {
                Cancellations = cancellations,
                TotalPages = totalPages,
                CurrentPage = page
            });
        }



        // GET: api/Cancellations/Detail/{id}
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


        // PUT: api/Cancellations/5
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

            // Cập nhật các trường cần thiết
            existingCancellation.BookingId = updatedCancellation.BookingId;
            existingCancellation.CancellationDate = updatedCancellation.CancellationDate;
            existingCancellation.RefundAmount = updatedCancellation.RefundAmount;

            // Lấy thông tin `Booking` từ `BookingId`
            var booking = await _context.Bookings.FindAsync(updatedCancellation.BookingId);
            if (booking == null)
            {
                return BadRequest(new { Message = "Booking không tồn tại." });
            }
            existingCancellation.Booking = booking;

            // Đánh dấu là đã chỉnh sửa
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
