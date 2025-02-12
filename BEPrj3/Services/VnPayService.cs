using BEPrj3.Libraries;
using BEPrj3.Models.Vnpay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace BEPrj3.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.UtcNow.Ticks.ToString(); // Định danh duy nhất cho giao dịch

            var pay = new VnPayLibrary();
            var returnUrl = _configuration["Vnpay:ReturnUrl"];

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
            pay.AddRequestData("vnp_TotalAmount", ((int)model.TotalAmount * 100).ToString()); // VNPay yêu cầu số tiền nhân 100
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toán vé xe {model.Name} - {model.OrderDescription}");
            pay.AddRequestData("vnp_OrderType", model.OrderType);
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);
            pay.AddRequestData("vnp_TxnRef", tick); // Mã giao dịch duy nhất
            pay.AddRequestData("vnp_OrderId", model.BookingId.ToString()); // ID của booking

            var paymentUrl = pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);
            return paymentUrl;
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var hashSecret = _configuration["Vnpay:HashSecret"];

            // Lấy dữ liệu phản hồi từ VNPay và kiểm tra tính hợp lệ
            var response = pay.GetFullResponseData(collections, hashSecret);

            // Kiểm tra chữ ký (secure hash) để đảm bảo không bị giả mạo
            if (!pay.ValidateSignature(collections, hashSecret))
            {
                response.Success = false;
                response.VnPayResponseCode = "97"; // Mã lỗi 97: Lỗi kiểm tra checksum
            }

            return response;
        }
    }
}
