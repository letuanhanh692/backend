using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class Cancellation
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public DateTime CancellationDate { get; set; }

    public decimal RefundAmount { get; set; }

    public virtual Booking? Booking { get; set; } = null!;
}
