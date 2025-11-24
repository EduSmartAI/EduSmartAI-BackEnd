namespace PaymentService.Application.Applications.Payments;

public class PaymentCallBackRequest
{
    public Guid OrderId { get; set; }
    
    public string Code { get; set; } // Return code from PayOS
    
    public string Id { get; set; } // Transaction ID from PayOS
    
    public bool Cancel { get; set; } // User cancelled payment
    
    public string Status { get; set; } // Payment status from PayOS
    
    public long OrderCode { get; set; } // OrderCode sent to PayOS
}

