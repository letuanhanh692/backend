using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class BusType
{
    public int Id { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Bus> Buses { get; set; } = new List<Bus>();

    public virtual ICollection<PriceList> PriceLists { get; set; } = new List<PriceList>();
}
