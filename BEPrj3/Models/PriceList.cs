﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BEPrj3.Models;

public partial class PriceList
{
    public int Id { get; set; }

    public int BusTypeId { get; set; }

    public int RouteId { get; set; }

    public decimal Price { get; set; }

    public virtual BusType? BusType { get; set; } = null!;

    [JsonIgnore]
    public virtual Route Route { get; set; }
}
