using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.WriteModels;

namespace PaymentService.Application.Applications.Payments.Commands.RePayment;

public class RePaymentCommandHandler(ICommandRepository<Order> orderRepository, IIdentityService identityService) : IRequestHandler<RePaymentCommand, RePaymentResponse>
{
    public async Task<RePaymentResponse> Handle(RePaymentCommand request, CancellationToken cancellationToken)
    {
        var response = new RePaymentResponse { Success = false };

        var currentUser = identityService.GetCurrentUser();
        if (currentUser == null)
        {
            response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
            return response;
        }

        // Get Order with PaymentTransactions
        var order = await orderRepository
            .Find(
                predicate: x => x.OrderId == request.OrderId && x.UserId == currentUser.UserId,
                isTracking: false,
                includes: o => o.PaymentTransactions)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đơn hàng");
            return response;
        }

        // Check if order is in WaitingForPayment status
        if (order.Status != (short)ConstantEnum.OrderStatus.WaitingForPayment)
        {
            response.SetMessage(MessageId.E00000, "Đơn hàng không ở trạng thái chờ thanh toán");
            return response;
        }

        // Get the pending payment transaction with PaymentUrl
        var pendingPayment = order.PaymentTransactions
            .Where(pt => pt.Status == (short)ConstantEnum.PaymentStatus.Pending 
                      && !string.IsNullOrEmpty(pt.PaymentUrl))
            .OrderByDescending(pt => pt.CreatedAt)
            .FirstOrDefault();

        if (pendingPayment == null || string.IsNullOrEmpty(pendingPayment.PaymentUrl))
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy liên kết thanh toán. Vui lòng tạo đơn hàng mới");
            return response;
        }

        response.Success = true;
        response.Response = new RePaymentResponseEntity
        {
            PaymentUrl = pendingPayment.PaymentUrl,
        };
        response.SetMessage(MessageId.I00001, "Lấy thông tin thanh toán thành công");

        return response;
    }
}

