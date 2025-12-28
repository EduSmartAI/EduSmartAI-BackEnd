using MediatR;

namespace PaymentService.Application.Applications.Payments.Commands.PaymentCallback;

public class PaymentCallbackCommand : IRequest<PaymentCallbackResponse>
{
    /// <summary>
    /// ID của đơn hàng
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Mã trả về từ PayOS (00 = success)
    /// </summary>
    public string Code { get; set; }
    
    /// <summary>
    /// Transaction ID từ PayOS
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// User có hủy thanh toán không
    /// </summary>
    public bool Cancel { get; set; }
    
    /// <summary>
    /// Trạng thái thanh toán từ PayOS
    /// </summary>
    public string Status { get; set; }
    
    /// <summary>
    /// Order code đã gửi cho PayOS
    /// </summary>
    public long OrderCode { get; set; }
}

