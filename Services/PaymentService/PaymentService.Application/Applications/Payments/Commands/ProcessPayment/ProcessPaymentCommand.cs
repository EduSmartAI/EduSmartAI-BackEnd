using MediatR;

namespace PaymentService.Application.Applications.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommand : IRequest<ProcessPaymentResponse>
{
    public Guid OrderId { get; set; }
}

