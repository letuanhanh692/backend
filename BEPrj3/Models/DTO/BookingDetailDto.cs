namespace BEPrj3.Models.DTO
{
    public class BookingDetailDto
    {
        public int BookingId { get; set; }  // ID của booking mà vé thuộc về

        public string Name { get; set; }  // Tên của khách hàng

        public int Age { get; set; }  // Tuổi của khách hàng

        public string Phone { get; set; }  // Số điện thoại của khách hàng

        public string Email { get; set; }  // Email của khách hàng

        public int SeatNumber { get; set; }
    }
}
