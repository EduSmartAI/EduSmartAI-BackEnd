using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Models;

namespace PaymentService.Application.Applications.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler(
    IPaymentServiceClient paymentServiceClient,
    ICommandRepository<Order> orderRepository,
    IIdentityService identityService) : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    public async Task<ProcessPaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var response = new ProcessPaymentResponse { Success = false };
        
        // Get current user
        var currentUser = identityService.GetCurrentUser();
        if (currentUser == null)
        {
            response.SetMessage(MessageId.E00000, "Người dùng không hợp lệ");
            return response;
        }
        
        // Get order and verify ownership
        var order = await orderRepository
            .Find(x => x.OrderId == request.OrderId)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (order == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đơn hàng");
            return response;
        }
        
        // Verify user owns this order
        if (order.UserId != currentUser.UserId)
        {
            response.SetMessage(MessageId.E00000, "Bạn không có quyền thanh toán đơn hàng này");
            return response;
        }
        
        // Process payment via PayOS
        var paymentResult = await paymentServiceClient.ProcessPaymentAsync(
            request.OrderId, 
            order.FinalAmount, 
            cancellationToken);
            
        if (!paymentResult.Success)
        {
            response.SetMessage(paymentResult.MessageId, paymentResult.Message);
            return response;
        }
        
        // Map response
        response.Response = new ProcessPaymentDto
        {
            CheckoutUrl = paymentResult.Response.CheckoutUrl,
            QrCode = paymentResult.Response.QrCode,
            TransactionId = paymentResult.Response.TransactionId,
            OrderId = request.OrderId
        };
        
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Tạo yêu cầu thanh toán thành công");
        return response;
    }
}

