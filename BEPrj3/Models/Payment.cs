using System;
using System.Collections.Generic;

namespace BEPrj3.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public string? Method { get; set; }

    public string? Status { get; set; }

    public string? PaymentCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
