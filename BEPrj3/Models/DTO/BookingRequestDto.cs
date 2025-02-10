namespace BEPrj3.Models.DTO
{
    public class BookingRequestDto
    {
        public int UserId { get; set; }  // ID người dùng

        public int ScheduleId { get; set; }  // ID lịch trình

        public string Name { get; set; }  // Tên khách hàng

        public int Age { get; set; }  // Tuổi khách hàng

        public string Phone { get; set; }  // Số điện thoại khách hàng

        public string Email { get; set; }  // Email khách hàng

        public int SeatNumber { get; set; }  // Số ghế khách hàng muốn đặt
    }

}
