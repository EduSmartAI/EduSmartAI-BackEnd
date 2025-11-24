using MediatR;

namespace PaymentService.Application.Applications.Orders.Queries.SelectOrder;

public class SelectOrderQuery : IRequest<SelectOrderResponse>
{
    public Guid? OrderId { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 10;
}

