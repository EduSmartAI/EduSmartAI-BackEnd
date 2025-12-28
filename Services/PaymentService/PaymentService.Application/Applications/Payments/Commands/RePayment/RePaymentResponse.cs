using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Payments.Commands.RePayment;

public record RePaymentResponse : AbstractApiResponse<RePaymentResponseEntity>
{
    public override RePaymentResponseEntity Response { get; set; }
}

public class RePaymentResponseEntity
{
    public string PaymentUrl { get; set; } = null!;
}

