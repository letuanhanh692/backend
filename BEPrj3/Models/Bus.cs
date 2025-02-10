using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BEPrj3.Models;

public partial class Bus
{
    public int Id { get; set; }

    public string BusNumber { get; set; } = null!;

    public int BusTypeId { get; set; }

    public int TotalSeats { get; set; }

    [Column("image_bus")]
    public string? ImageBus {  get; set; }

    public virtual BusType? BusType { get; set; } = null!;

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
