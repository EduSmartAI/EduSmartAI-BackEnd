using System;
using System.Collections.Generic;

namespace PaymentService.Domain.WriteModels;

public partial class Cart
{
    public Guid CartId { get; set; }

    public Guid UserId { get; set; }

    public short Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
