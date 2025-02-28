namespace BEPrj3.Models.DTO
{
    public class BookingResponseDto
    {
        public int BookingId { get; set; }  // ID của booking

        // Thông tin cá nhân khách hàng
        public string Name { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        // Thông tin đặt chỗ
        public int SeatNumber { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        // Thông tin chuyến đi
        public string BusNumber { get; set; }
        public string BusType { get; set; }
        public DateTime DepartTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string StartingPlace { get; set; }
        public string DestinationPlace { get; set; }
        public double Distance { get; set; }
        public int UserId { get; internal set; }
        public int ScheduleId { get; internal set; }
        public decimal RefundAmount { get; internal set; }
    }
}
