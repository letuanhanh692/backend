namespace BEPrj3.Models.DTO
{
    public class ScheduleRequest
    {
        public int Id { get; set; }
        public int BusId { get; set; }
        public int RouteId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime Date { get; set; } // Có thể không dùng đến trong logic hiện tại nhưng để giữ tính linh hoạt
    }

}
