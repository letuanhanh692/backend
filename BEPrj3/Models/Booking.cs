using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ScheduleId { get; set; }

    public int SeatNumber { get; set; }

    public int Age { get; set; }

    public DateTime? BookingDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Cancellation> Cancellations { get; set; } = new List<Cancellation>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Schedule Schedule { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public string? Name { get; set; } 
}
