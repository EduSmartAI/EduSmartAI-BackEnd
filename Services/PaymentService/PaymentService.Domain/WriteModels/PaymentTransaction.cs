using System;
using System.Collections.Generic;

namespace PaymentService.Domain.WriteModels;

public partial class PaymentTransaction
{
    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public string Gateway { get; set; } = null!;

    public string? GatewayTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public short Status { get; set; }

    public string? ReturnCode { get; set; }

    public string? ReturnMessage { get; set; }

    public string? RawResponse { get; set; }

    public string? ClientIp { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? PaymentUrl { get; set; }

    public bool IsActive { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
