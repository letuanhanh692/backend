namespace BEPrj3.Models.DTO
{
    public class PaymentInformationModel
    {
        public int BookingId { get; set; } // ID đơn đặt vé
        public int UserId { get; set; } // ID người đặt vé (Users)
        public int ScheduleId { get; set; } // ID lịch trình (Schedules)
        public int SeatNumber { get; set; } // Số ghế đặt
        public int Age { get; set; } // Tuổi hành khách
        public DateTime BookingDate { get; set; } // Ngày đặt vé
        public decimal TotalAmount { get; set; } // Tổng tiền thanh toán
        public string BookingStatus { get; set; } // Trạng thái đặt vé (Booked, Cancelled, Completed)

        // Thông tin người dùng (Users)
        public string UserName { get; set; } // Tên người dùng
        public string Email { get; set; } // Email liên hệ
        public string Phone { get; set; } // Số điện thoại
        public string Address { get; set; } // Địa chỉ
        public string IdCard { get; set; } // CMND/CCCD

        // Thông tin thanh toán (Payments)
        public int PaymentId { get; set; } // ID thanh toán
        public decimal PaymentAmount { get; set; } // Số tiền đã thanh toán
        public string PaymentMethod { get; set; } // Phương thức thanh toán (Cash, Card, Transfer)
        public string PaymentStatus { get; set; } // Trạng thái thanh toán (Pending, Completed, Failed)
        public string PaymentCode { get; set; } // Mã giao dịch thanh toán
        public DateTime PaymentDate { get; set; } // Ngày thanh toán
    }
}
