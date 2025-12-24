using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Payments.Queries.PaymentHistory;

/// <summary>
/// Response trả về danh sách lịch sử thanh toán
/// </summary>
public record PaymentHistorySelectQueryResponse : AbstractApiResponse<PaymentHistoryResponseData>
{
    public override PaymentHistoryResponseData Response { get; set; } = null!;
}

/// <summary>
/// Data trả về bao gồm phân trang
/// </summary>
public class PaymentHistoryResponseData
{
    /// <summary>
    /// Danh sách lịch sử thanh toán
    /// </summary>
    public List<PaymentHistoryItem> Items { get; set; } = new();
    
    /// <summary>
    /// Tổng số bản ghi
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Số trang hiện tại
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Số lượng bản ghi mỗi trang
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Tổng số trang
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Chi tiết một giao dịch thanh toán
/// </summary>
public class PaymentHistoryItem
{
    /// <summary>
    /// ID giao dịch thanh toán
    /// </summary>
    public Guid PaymentId { get; set; }
    
    /// <summary>
    /// ID đơn hàng
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Cổng thanh toán (VNPay, PayOS, etc.)
    /// </summary>
    public string Gateway { get; set; } = null!;
    
    /// <summary>
    /// Mã giao dịch từ cổng thanh toán
    /// </summary>
    public string? GatewayTransactionId { get; set; }
    
    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Đơn vị tiền tệ
    /// </summary>
    public string Currency { get; set; } = null!;
    
    /// <summary>
    /// Trạng thái thanh toán
    /// </summary>
    public short Status { get; set; }
    
    /// <summary>
    /// Tên trạng thái thanh toán
    /// </summary>
    public string StatusName { get; set; } = null!;
    
    /// <summary>
    /// Mã trả về từ cổng thanh toán
    /// </summary>
    public string? ReturnCode { get; set; }
    
    /// <summary>
    /// Thông báo trả về từ cổng thanh toán
    /// </summary>
    public string? ReturnMessage { get; set; }
    
    /// <summary>
    /// Thời gian tạo giao dịch
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật giao dịch
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Thông tin đơn hàng
    /// </summary>
    public PaymentOrderInfo? OrderInfo { get; set; }
}

/// <summary>
/// Thông tin đơn hàng liên quan đến thanh toán
/// </summary>
public class PaymentOrderInfo
{
    /// <summary>
    /// Tổng tiền trước giảm giá
    /// </summary>
    public decimal SubtotalAmount { get; set; }
    
    /// <summary>
    /// Số tiền giảm giá
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Số tiền cuối cùng
    /// </summary>
    public decimal FinalAmount { get; set; }
    
    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Thời gian thanh toán thành công
    /// </summary>
    public DateTime? PaidAt { get; set; }
}

