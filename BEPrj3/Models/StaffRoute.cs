using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class StaffRoute
{
    public int Id { get; set; }

    public int StaffId { get; set; }

    public int RouteId { get; set; }

    public virtual Route Route { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
