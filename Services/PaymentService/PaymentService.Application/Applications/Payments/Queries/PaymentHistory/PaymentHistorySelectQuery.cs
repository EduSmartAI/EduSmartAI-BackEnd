using BuildingBlocks.CQRS;

namespace PaymentService.Application.Applications.Payments.Queries.PaymentHistory;

/// <summary>
/// Query để lấy lịch sử thanh toán của người dùng
/// </summary>
public class PaymentHistorySelectQuery : IQuery<PaymentHistorySelectQueryResponse>
{
    /// <summary>
    /// ID của người dùng (nếu null sẽ lấy từ token)
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Số trang (mặc định = 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Số lượng bản ghi mỗi trang (mặc định = 10)
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Trạng thái thanh toán (null = tất cả)
    /// </summary>
    public short? Status { get; set; }
    
    /// <summary>
    /// Ngày bắt đầu lọc
    /// </summary>
    public DateOnly? FromDate { get; set; }
    
    /// <summary>
    /// Ngày kết thúc lọc
    /// </summary>
    public DateOnly? ToDate { get; set; }
}

