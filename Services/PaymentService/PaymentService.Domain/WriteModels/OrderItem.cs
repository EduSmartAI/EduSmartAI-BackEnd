using System;
using System.Collections.Generic;

namespace PaymentService.Domain.WriteModels;

public partial class OrderItem
{
    public Guid OrderItemId { get; set; }

    public Guid OrderId { get; set; }

    public Guid CourseId { get; set; }

    public string CourseTitleSnapshot { get; set; } = null!;

    public string? CourseImageUrlSnapshot { get; set; }

    public decimal PriceSnapshot { get; set; }

    public decimal? DealPriceSnapshot { get; set; }

    public decimal FinalPrice { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Order Order { get; set; } = null!;
}
