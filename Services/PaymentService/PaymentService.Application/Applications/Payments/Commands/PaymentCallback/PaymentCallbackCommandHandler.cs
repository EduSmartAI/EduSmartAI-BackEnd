using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using PaymentService.Application.Interfaces;
namespace PaymentService.Application.Applications.Payments.Commands.PaymentCallback;

public class PaymentCallbackCommandHandler(
    IPaymentServiceClient paymentServiceClient,
    IIdentityService identityService) : IRequestHandler<PaymentCallbackCommand, PaymentCallbackResponse>
{
    public async Task<PaymentCallbackResponse> Handle(PaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        var response = new PaymentCallbackResponse { Success = false };
        
        // Get current user
        var currentUser = identityService.GetCurrentUser()!;
        
        // Map command to request
        var callbackRequest = new PaymentCallBackRequest
        {
            OrderId = request.OrderId,
            Code = request.Code,
            Id = request.Id,
            Cancel = request.Cancel,
            Status = request.Status,
            OrderCode = request.OrderCode
        };
        
        // Process callback
        var callbackResult = await paymentServiceClient.PaymentCallbackAsync(callbackRequest, currentUser);
        
        if (!callbackResult.Success)
        {
            response.SetMessage(callbackResult.MessageId, callbackResult.Message);
            
            // If there's a retry payment link, include it
            if (callbackResult.Response != null)
            {
                response.Response = new PaymentCallbackDto
                {
                    CheckoutUrl = callbackResult.Response.CheckoutUrl,
                    QrCode = callbackResult.Response.QrCode,
                    OrderId = request.OrderId,
                    Message = callbackResult.Message
                };
            }
            
            return response;
        }
        
        // Success response
        response.Response = new PaymentCallbackDto
        {
            OrderId = request.OrderId,
            OrderStatus = "Paid",
            Message = callbackResult.Message
        };
        
        // True
        response.Success = true;
        response.SetMessage(callbackResult.MessageId, "Thanh toán");
        return response;
    }
}

