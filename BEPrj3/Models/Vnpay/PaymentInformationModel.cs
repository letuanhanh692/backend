namespace BEPrj3.Models.Vnpay
{
    public class PaymentInformationModel
    {
        public int BookingId { get; set; } // Liên kết với ID của booking
        public string OrderType { get; set; }
        public double TotalAmount { get; set; }
        public string OrderDescription { get; set; }
        public string Name { get; set; }
        public int SeatCount { get; internal set; }
    }
}
