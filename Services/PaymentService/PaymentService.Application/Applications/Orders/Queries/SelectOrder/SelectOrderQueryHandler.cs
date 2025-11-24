using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.WriteModels;

namespace PaymentService.Application.Applications.Orders.Queries.SelectOrder;

public class SelectOrderQueryHandler(ICommandRepository<Order> orderRepository, IIdentityService identityService) : IRequestHandler<SelectOrderQuery, SelectOrderResponse>
{
    public async Task<SelectOrderResponse> Handle(SelectOrderQuery request, CancellationToken cancellationToken)
    {
        var response = new SelectOrderResponse { Success = false };

        var currentUser = identityService.GetCurrentUser()!;

        // If OrderId is provided, get single order
        if (request.OrderId.HasValue)
        {
            var order = await orderRepository
                .Find(
                    predicate: o => o.OrderId == request.OrderId.Value && o.UserId == currentUser.UserId,
                    isTracking: false,
                    includes: o => o.OrderItems)
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy đơn hàng");
                return response;
            }

            var orderEntity = MapToOrderEntity(order);

            response.Success = true;
            response.Response = new List<SelectOrderResponseEntity> { orderEntity };
            response.TotalRecords = 1;
            response.PageIndex = 0;
            response.PageSize = 1;
            response.SetMessage(MessageId.I00001, "Lấy thông tin đơn hàng thành công");
            return response;
        }

        // Get paginated list of orders
        var query = orderRepository
            .Find(
                predicate: o => o.UserId == currentUser.UserId && o.IsActive,
                isTracking: false,
                includes: o => o.OrderItems)
            .OrderByDescending(o => o.CreatedAt);

        var totalRecords = await query.CountAsync(cancellationToken);

        var orders = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var orderEntities = orders.Select(MapToOrderEntity).ToList();

        response.Success = true;
        response.Response = orderEntities;
        response.TotalRecords = totalRecords;
        response.PageIndex = request.PageIndex;
        response.PageSize = request.PageSize;
        response.SetMessage(MessageId.I00001, "Lấy danh sách đơn hàng thành công");

        return response;
    }

    private static SelectOrderResponseEntity MapToOrderEntity(Order order)
    {
        var statusName = GetOrderStatusName(order.Status);

        return new SelectOrderResponseEntity
        {
            OrderId = order.OrderId,
            OrderDate = order.CreatedAt, // Use CreatedAt as OrderDate
            Status = order.Status,
            StatusName = statusName,
            Subtotal = order.SubtotalAmount,
            Discount = order.DiscountAmount,
            FinalAmount = order.FinalAmount,
            Currency = order.Currency,
            PaidAt = order.PaidAt,
            OrderItems = order.OrderItems?.Select(oi => new OrderItemEntity
            {
                OrderItemId = oi.OrderItemId,
                CourseId = oi.CourseId,
                Price = oi.PriceSnapshot,
                Discount = oi.PriceSnapshot - (oi.DealPriceSnapshot ?? oi.PriceSnapshot),
                FinalPrice = oi.FinalPrice
            }).ToList() ?? new List<OrderItemEntity>()
        };
    }

    private static string GetOrderStatusName(short status)
    {
        return status switch
        {
            0 => ConstantEnum.OrderStatus.Pending.GetDescription(),
            1 => ConstantEnum.OrderStatus.WaitingForPayment.GetDescription(),
            2 => ConstantEnum.OrderStatus.Paid.GetDescription(),
            3 => ConstantEnum.OrderStatus.Cancelled.GetDescription(),
            4 => ConstantEnum.OrderStatus.Failed.GetDescription(),
            _ => "Unknown"
        };
    }
}

