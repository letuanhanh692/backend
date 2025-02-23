namespace BEPrj3.Models.DTO
{
    public class PaymentResponseModel
    {
        public int PaymentId { get; set; } // ID thanh toán
        public int BookingId { get; set; } // ID đơn đặt vé
        public decimal PaymentAmount { get; set; } // Số tiền đã thanh toán
        public string PaymentMethod { get; set; } // Phương thức thanh toán (Cash, Card, Transfer)
        public string PaymentStatus { get; set; } // Trạng thái thanh toán (Pending, Completed, Failed)
        public string PaymentCode { get; set; } // Mã giao dịch thanh toán
        public DateTime PaymentDate { get; set; } // Ngày thanh toán

        // Thông tin đơn đặt vé (tuỳ chọn)
        public string BookingStatus { get; set; } // Trạng thái đơn đặt vé
        public int SeatNumber { get; set; } // Số ghế đã đặt
        public DateTime BookingDate { get; set; } // Ngày đặt vé

        // Phản hồi từ hệ thống
        public string Message { get; set; } // Thông điệp phản hồi (ví dụ: "Thanh toán thành công")
        public bool Success { get; set; } // Trạng thái phản hồi (true = thành công, false = thất bại)
    }
}
