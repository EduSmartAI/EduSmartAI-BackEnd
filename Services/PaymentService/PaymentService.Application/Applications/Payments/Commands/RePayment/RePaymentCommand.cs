using MediatR;

namespace PaymentService.Application.Applications.Payments.Commands.RePayment;

public class RePaymentCommand : IRequest<RePaymentResponse>
{
    public Guid OrderId { get; set; }
}

