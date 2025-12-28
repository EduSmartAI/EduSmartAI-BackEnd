using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Payments.Commands.ProcessPayment;

public record ProcessPaymentResponse : AbstractApiResponse<ProcessPaymentDto>
{
    public override ProcessPaymentDto Response { get; set; }
}

public class ProcessPaymentDto
{
    /// <summary>
    /// URL để redirect user tới trang thanh toán
    /// </summary>
    public string CheckoutUrl { get; set; }
    
    /// <summary>
    /// QR code để user có thể scan thanh toán
    /// </summary>
    public string QrCode { get; set; }
    
    /// <summary>
    /// Mã giao dịch từ PayOS
    /// </summary>
    public string TransactionId { get; set; }
    
    /// <summary>
    /// ID của đơn hàng
    /// </summary>
    public Guid OrderId { get; set; }
}

