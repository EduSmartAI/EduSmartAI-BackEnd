using PaymentService.Application.Interfaces;

namespace PaymentService.Application.Applications.Orders.Commands.InsertOrder;

public record InsertOrderResponse : PaymentResponse
{
    public Guid? OrderId { get; set; }
}

