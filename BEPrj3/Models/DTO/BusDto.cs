namespace BEPrj3.Models.DTO
{
    public class BusDto
    {
        public string BusNumber { get; set; } = string.Empty;
        public int BusTypeId { get; set; }
        public int TotalSeats { get; set; }
        public IFormFile? File { get; set; } 
    }
}
