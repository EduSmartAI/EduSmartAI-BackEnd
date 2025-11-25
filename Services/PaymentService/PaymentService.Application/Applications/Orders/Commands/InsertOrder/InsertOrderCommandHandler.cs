using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.WriteModels;

namespace PaymentService.Application.Applications.Orders.Commands.InsertOrder;

public class InsertOrderCommandHandler(
    ICommandRepository<Order> orderRepository,
    ICommandRepository<OrderItem> orderItemRepository,
    ICommandRepository<Cart> cartRepository,
    ICommandRepository<CartItem> cartItemRepository,
    IUnitOfWork unitOfWork,
    IIdentityService identityService,
    IPaymentServiceClient paymentServiceClient) : IRequestHandler<InsertOrderCommand, InsertOrderResponse>
{
    public async Task<InsertOrderResponse> Handle(InsertOrderCommand request, CancellationToken cancellationToken)
    {
        var response = new InsertOrderResponse { Success = false };

        var currentUser = identityService.GetCurrentUser()!;
        
        // Validate that cartItemIds are provided
        if (!request.CartItemIds.Any())
        {
            response.SetMessage(MessageId.E00000, "Danh sách sản phẩm trong giỏ hàng không được để trống");
            return response;
        }

        // Get user's cart with items
        var cart = await cartRepository
            .Find(
                predicate: c => c.UserId == currentUser.UserId,
                isTracking: false,
                includes: c => c.CartItems)
            .FirstOrDefaultAsync(cancellationToken);

        if (cart == null || !cart.CartItems.Any())
        {
            response.SetMessage(MessageId.E00000, "Giỏ hàng trống");
            return response;
        }

        // Filter cart items by requested cartItemIds
        var cartItemsToOrder = cart.CartItems
            .Where(ci => request.CartItemIds.Contains(ci.CartItemId) && ci.IsActive)
            .ToList();

        if (!cartItemsToOrder.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy sản phẩm trong giỏ hàng");
            return response;
        }

        // Calculate order totals based on CartItem schema
        // PriceSnapshot is original price, DealPriceSnapshot is discounted price (nullable)
        var subtotal = cartItemsToOrder.Sum(ci => ci.PriceSnapshot);
        var discount = cartItemsToOrder.Sum(ci => ci.PriceSnapshot - (ci.DealPriceSnapshot ?? ci.PriceSnapshot));
        var finalAmount = cartItemsToOrder.Sum(ci => ci.DealPriceSnapshot ?? ci.PriceSnapshot);

        if (finalAmount <= 0)
        {
            response.SetMessage(MessageId.E00000, "Tổng tiền đơn hàng phải lớn hơn 0");
            return response;
        }

        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Create Order based on Order schema
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                Status = (short) ConstantEnum.OrderStatus.Pending,
                SubtotalAmount = subtotal,
                DiscountAmount = discount,
                FinalAmount = finalAmount,
                Currency = "VND",
                PaymentMethod = request.PaymentMethod.ToString(),
                PaymentDueAt = DateTime.UtcNow.AddHours(24),
                PaidAt = null,
                OrderItems = new List<OrderItem>()
            };

            // Create OrderItems from CartItems based on OrderItem schema
            foreach (var cartItem in cartItemsToOrder)
            {
                var orderItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    CourseId = cartItem.CourseId,
                    CourseTitleSnapshot = cartItem.CourseTitleSnapshot,
                    CourseImageUrlSnapshot = cartItem.CourseImageUrlSnapshot,
                    PriceSnapshot = cartItem.PriceSnapshot,
                    DealPriceSnapshot = cartItem.DealPriceSnapshot,
                    FinalPrice = cartItem.DealPriceSnapshot ?? cartItem.PriceSnapshot,
                    Quantity = 1,
                };
                order.OrderItems.Add(orderItem);
            }
            
            // Save order and order items
            await orderRepository.AddAsync(order);
            await orderItemRepository.AddRangeAsync(order.OrderItems);
            await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            // Remove purchased items from cart
            var cartItemsToRemove = await cartItemRepository
                .Find(ci => ci.CartId == cart.CartId && request.CartItemIds.Contains(ci.CartItemId))
                .ToListAsync(cancellationToken);

            if (cartItemsToRemove.Any())
            {
                foreach (var item in cartItemsToRemove)
                {
                    cartItemRepository.Update(item!);
                }
                await unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, true);
            }

            // Process payment immediately after order creation
            var paymentResponse = await paymentServiceClient.ProcessPaymentAsync(order, cancellationToken);
            if (!paymentResponse.Success)
            {
                response.Success = false;
                response.MessageId = paymentResponse.MessageId;
                response.Message = paymentResponse.Message;
            }

            // Return successful response with payment info
            response.Success = true;
            response.OrderId = order.OrderId;
            response.Response = paymentResponse.Response;
            response.SetMessage(MessageId.I00001, "Tạo đơn hàng");
            return true;
        }, cancellationToken);
        return response;
    }
}

