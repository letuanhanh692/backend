using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class Schedule
{
    public int Id { get; set; }

    public int BusId { get; set; }

    public int RouteId { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public DateOnly Date { get; set; }

    public int AvailableSeats { get; set; }

    public decimal? Price { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Bus? Bus { get; set; } = null!;

    public virtual Route? Route { get; set; } = null!;

}
