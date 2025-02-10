namespace BEPrj3.Models.DTO
{
    public class RouteDTO
    {
        public int? Id { get; set; }
        public string StartingPlace { get; set; }
        public string DestinationPlace { get; set; }
        public decimal Distance { get; set; }
        public decimal PriceRoute { get; set; }
        public int StaffId { get; set; }

        public string StaffName { get; set; }
        public string StaffEmail { get; set; }
    }
}
