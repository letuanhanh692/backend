﻿using BEPrj3.Libaries;
using BEPrj3.Models.DTO;

namespace BEPrj3.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly string _vnpUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private readonly string _vnpTmnCode = "YOUR_TMNCODE";
        private readonly string _vnpHashSecret = "YOUR_HASH_SECRET";
        private readonly string _returnUrl = "https://yourdomain.com/vnpay_return";

        public string CreatePaymentUrl(PaymentInformationModel model, string baseUrl, string paymentCode)
        {
            VnPayLibrary vnPay = new VnPayLibrary();
            vnPay.AddRequestData("vnp_Version", "2.1.0");
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", _vnpTmnCode);
            vnPay.AddRequestData("vnp_Amount", ((int)(model.TotalAmount * 100)).ToString());
            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_TxnRef", paymentCode); // Dùng paymentCode thay vì BookingId
            vnPay.AddRequestData("vnp_OrderInfo", $"Thanh toán đơn đặt vé #{paymentCode}");
            vnPay.AddRequestData("vnp_OrderType", "billpayment");
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_ReturnUrl", _returnUrl);
            vnPay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnPay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            return vnPay.CreateRequestUrl(_vnpUrl, _vnpHashSecret);
        }


        public PaymentResponseModel ProcessPaymentResponse(Dictionary<string, string> responseParams)
        {
            VnPayLibrary vnPay = new VnPayLibrary();
            foreach (var param in responseParams)
            {
                vnPay.AddResponseData(param.Key, param.Value);
            }

            bool isValid = vnPay.ValidateSignature(_vnpHashSecret);

            return new PaymentResponseModel
            {
                PaymentId = isValid ? Convert.ToInt32(responseParams["vnp_TxnRef"]) : 0,
                BookingId = isValid ? Convert.ToInt32(responseParams["vnp_TxnRef"]) : 0,
                PaymentAmount = isValid ? Convert.ToDecimal(responseParams["vnp_Amount"]) / 100 : 0,
                PaymentMethod = responseParams.ContainsKey("vnp_CardType") ? responseParams["vnp_CardType"] : "Unknown",
                PaymentStatus = isValid ? "Completed" : "Failed",
                PaymentCode = responseParams.ContainsKey("vnp_TransactionNo") ? responseParams["vnp_TransactionNo"] : null,
                PaymentDate = DateTime.Now,
                BookingStatus = isValid ? "Booked" : "Failed",
                SeatNumber = isValid ? 1 : 0, // Giả định số ghế được đặt
                BookingDate = DateTime.Now,
                Message = isValid ? "Thanh toán thành công" : "Thanh toán thất bại",
                Success = isValid
            };
        }
    }
}
