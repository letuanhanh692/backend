using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BEPrj3.Models;
using BEPrj3.Models.DTO;
using BEPrj3.Services;

namespace BEPrj3.Controllers.Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;
        private readonly BusBookingContext _context;

        public PaymentController(IVnPayService vnPayService, IConfiguration configuration, BusBookingContext context)
        {
            _vnPayService = vnPayService;
            _configuration = configuration;
            _context = context;
        }

        // API tạo thanh toán
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentInformationModel model)
        {
            if (model == null || model.TotalAmount <= 0)
                return BadRequest(new { message = "Thông tin thanh toán không hợp lệ." });

            try
            {
                var bookingExists = await _context.Bookings.AnyAsync(b => b.Id == model.BookingId);
                if (!bookingExists)
                    return BadRequest(new { message = "Mã đặt vé không tồn tại." });

                // Tạo PaymentCode tự động
                string paymentCode = GeneratePaymentCode();
                while (await _context.Payments.AnyAsync(p => p.PaymentCode == paymentCode))
                {
                    paymentCode = GeneratePaymentCode(); // Đảm bảo PaymentCode là duy nhất
                }

                // Tạo thanh toán mới
                var payment = new BEPrj3.Models.Payment
                {
                    BookingId = model.BookingId,
                    Amount = model.TotalAmount,
                    Method = model.PaymentMethod,
                    Status = "Pending",
                    PaymentCode = paymentCode, // Gán PaymentCode tự động tạo
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Tạo URL thanh toán từ VnPay
                string baseUrl = _configuration["Vnpay:BaseUrl"];
                string paymentUrl = _vnPayService.CreatePaymentUrl(model, baseUrl, paymentCode);

                return Ok(new { paymentUrl, paymentId = payment.Id, paymentCode = payment.PaymentCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo thanh toán.", error = ex.Message });
            }
        }

        // Hàm tạo PaymentCode tự động
        private string GeneratePaymentCode()
        {
            return $"PAY-{new Random().Next(100000, 999999)}";
        }

        // API xử lý kết quả thanh toán
        [HttpGet("payment-callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            var responseParams = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var responseModel = _vnPayService.ProcessPaymentResponse(responseParams);

            try
            {
                // Tìm giao dịch thanh toán theo PaymentCode
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentCode == responseModel.PaymentCode);

                if (payment == null)
                    return NotFound(new { message = "Không tìm thấy giao dịch thanh toán." });

                // Cập nhật trạng thái thanh toán
                payment.Status = responseModel.Success ? "Completed" : "Failed";

                // Nếu thanh toán thành công, cập nhật trạng thái Booking thành "Completed"
                if (responseModel.Success)
                {
                    var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == payment.BookingId);
                    if (booking != null && booking.Status == "Booked") // Chỉ cập nhật nếu chưa bị hủy
                    {
                        booking.Status = "Completed";
                    }
                }

                await _context.SaveChangesAsync();

                return Redirect($"http://localhost:4200/user/success");
            }
            catch (Exception ex)
            {
                return Redirect($"http://localhost:4200/fail");
            }
        }

    }
}
