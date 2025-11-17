using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.ApiEntities;
using PaymentService.Application.Applications.Payments;

namespace PaymentService.Application.Interfaces;

public interface IPaymentServiceClient
{
    Task<PaymentResponse> ProcessPaymentAsync(Guid paymentId, decimal amount, CancellationToken ct = default);
    Task<bool> RefundPaymentAsync(string transactionId, CancellationToken ct = default);

    Task<PaymentCallbackResponse> PaymentCallbackAsync(PaymentCallBackRequest request, IdentityEntity identityEntity);
}

public record PaymentResponse : AbstractApiResponse<PaymentResultEntity>
{
    public override PaymentResultEntity Response { get; set; }
}

public class PaymentResultEntity
{
    public string TransactionId { get; set; }
    
    public string CheckoutUrl { get; set; }
    
    public string QrCode { get; set; }
}
