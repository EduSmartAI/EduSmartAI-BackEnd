using System;
using System.Collections.Generic;

namespace PaymentService.Domain.WriteModels;

public partial class CartItem
{
    public Guid CartItemId { get; set; }

    public Guid CartId { get; set; }

    public Guid CourseId { get; set; }

    public string CourseTitleSnapshot { get; set; } = null!;

    public string? CourseImageUrlSnapshot { get; set; }

    public decimal PriceSnapshot { get; set; }

    public decimal? DealPriceSnapshot { get; set; }

    public bool IsSelected { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Cart Cart { get; set; } = null!;
}
