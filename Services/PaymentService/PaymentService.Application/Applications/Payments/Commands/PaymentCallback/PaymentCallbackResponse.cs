using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Payments.Commands.PaymentCallback;

public record PaymentCallbackResponse : AbstractApiResponse<PaymentCallbackDto>
{
    public override PaymentCallbackDto Response { get; set; }
}

public class PaymentCallbackDto
{
    /// <summary>
    /// URL thanh toán mới (trong trường hợp thanh toán thất bại và cần thử lại)
    /// </summary>
    public string CheckoutUrl { get; set; }
    
    /// <summary>
    /// QR code mới (trong trường hợp thanh toán thất bại và cần thử lại)
    /// </summary>
    public string QrCode { get; set; }
    
    /// <summary>
    /// ID của đơn hàng
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Trạng thái đơn hàng sau khi xử lý callback
    /// </summary>
    public string OrderStatus { get; set; }
    
    /// <summary>
    /// Message mô tả kết quả
    /// </summary>
    public string Message { get; set; }
}

