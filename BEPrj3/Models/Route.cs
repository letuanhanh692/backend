using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class Route
{
    public int Id { get; set; }

    public string StartingPlace { get; set; } = null!;

    public string DestinationPlace { get; set; } = null!;
    public decimal? PriceRoute { get; set; }

    public decimal Distance { get; set; }

    

    public virtual ICollection<PriceList> PriceLists { get; set; } = new List<PriceList>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<StaffRoute> StaffRoutes { get; set; } = new List<StaffRoute>();
}
