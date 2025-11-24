using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Payments;

public record PaymentCallbackResponse : AbstractApiResponse<PaymentCallbackEntity>
{
    public override PaymentCallbackEntity Response { get; set; }
}

public class PaymentCallbackEntity
{
    public string CheckoutUrl { get; set; }
    
    public string QrCode { get; set; }
}

