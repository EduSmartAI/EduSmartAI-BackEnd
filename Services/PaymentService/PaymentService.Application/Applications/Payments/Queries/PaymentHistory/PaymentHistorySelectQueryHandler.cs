using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.WriteModels;

namespace PaymentService.Application.Applications.Payments.Queries.PaymentHistory;

/// <summary>
/// Handler xử lý query lấy lịch sử thanh toán của người dùng
/// </summary>
public class PaymentHistorySelectQueryHandler(
    ICommandRepository<PaymentTransaction> paymentTransactionRepository,
    IIdentityService identityService) : IQueryHandler<PaymentHistorySelectQuery, PaymentHistorySelectQueryResponse>
{
    public async Task<PaymentHistorySelectQueryResponse> Handle(PaymentHistorySelectQuery request, CancellationToken cancellationToken)
    {
        var response = new PaymentHistorySelectQueryResponse { Success = false };

        // Validate PageNumber
        if (request.PageNumber < 1)
        {
            response.SetMessage(MessageId.E00000, "Số trang phải lớn hơn hoặc bằng 1");
            return response;
        }

        // Validate PageSize
        if (request.PageSize < 1)
        {
            response.SetMessage(MessageId.E00000, "Số lượng bản ghi mỗi trang phải lớn hơn hoặc bằng 1");
            return response;
        }

        if (request.PageSize > 100)
        {
            response.SetMessage(MessageId.E00000, "Số lượng bản ghi mỗi trang không được vượt quá 100");
            return response;
        }

        // Validate Status (nếu có)
        if (request.Status.HasValue)
        {
            var validStatuses = new[] { 
                (short)ConstantEnum.PaymentStatus.Pending, 
                (short)ConstantEnum.PaymentStatus.Paid, 
                (short)ConstantEnum.PaymentStatus.Failed, 
                (short)ConstantEnum.PaymentStatus.SystemError 
            };
            
            if (!validStatuses.Contains(request.Status.Value))
            {
                response.SetMessage(MessageId.E00000, "Trạng thái thanh toán không hợp lệ. Giá trị hợp lệ: 1 (Pending), 2 (Paid), 3 (Failed), 4 (SystemError)");
                return response;
            }
        }

        // Validate FromDate và ToDate
        if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            if (request.FromDate.Value > request.ToDate.Value)
            {
                response.SetMessage(MessageId.E00000, "Ngày bắt đầu không được lớn hơn ngày kết thúc");
                return response;
            }
        }

        // Validate ngày không được trong tương lai
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.FromDate.HasValue && request.FromDate.Value > today)
        {
            response.SetMessage(MessageId.E00000, "Ngày bắt đầu không được là ngày trong tương lai");
            return response;
        }

        // Lấy UserId từ request hoặc từ token
        var userId = request.UserId ?? identityService.GetCurrentUser()?.UserId;
        
        if (userId == null || userId == Guid.Empty)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin người dùng");
            return response;
        }

        // Build query
        var query = paymentTransactionRepository
            .Find(x => x.IsActive && x.Order.UserId == userId.Value)
            .Include(x => x.Order)
            .AsQueryable();

        // Filter theo status
        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        // Filter theo ngày (chuyển DateOnly sang DateTime)
        if (request.FromDate.HasValue)
        {
            var fromDateTime = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.CreatedAt >= fromDateTime);
        }

        if (request.ToDate.HasValue)
        {
            // Lấy cuối ngày (23:59:59.9999999)
            var toDateTime = request.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.CreatedAt <= toDateTime);
        }

        // Đếm tổng số bản ghi
        var totalCount = await query.CountAsync(cancellationToken);

        // Phân trang và sắp xếp theo thời gian mới nhất
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PaymentHistoryItem
            {
                PaymentId = x.PaymentId,
                OrderId = x.OrderId,
                Gateway = x.Gateway,
                GatewayTransactionId = x.GatewayTransactionId,
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status,
                StatusName = GetPaymentStatusName(x.Status),
                ReturnCode = x.ReturnCode,
                ReturnMessage = x.ReturnMessage,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                OrderInfo = new PaymentOrderInfo
                {
                    SubtotalAmount = x.Order.SubtotalAmount,
                    DiscountAmount = x.Order.DiscountAmount,
                    FinalAmount = x.Order.FinalAmount,
                    PaymentMethod = x.Order.PaymentMethod,
                    PaidAt = x.Order.PaidAt
                }
            })
            .ToListAsync(cancellationToken);

        // Build response
        response.Success = true;
        response.Response = new PaymentHistoryResponseData
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        response.SetMessage(MessageId.I00001, "Lấy lịch sử thanh toán thành công");
        return response;
    }

    /// <summary>
    /// Chuyển đổi status code thành tên hiển thị
    /// </summary>
    private static string GetPaymentStatusName(short status)
    {
        return status switch
        {
            (short)ConstantEnum.PaymentStatus.Pending => "Đang chờ thanh toán",
            (short)ConstantEnum.PaymentStatus.Paid => "Đã thanh toán",
            (short)ConstantEnum.PaymentStatus.Failed => "Thanh toán thất bại",
            (short)ConstantEnum.PaymentStatus.SystemError => "Lỗi hệ thống",
            _ => "Không xác định"
        };
    }
}

