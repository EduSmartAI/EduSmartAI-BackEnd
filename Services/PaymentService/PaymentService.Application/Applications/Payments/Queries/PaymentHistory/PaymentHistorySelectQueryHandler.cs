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

        // Filter theo ngày
        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            var toDateEnd = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CreatedAt <= toDateEnd);
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

