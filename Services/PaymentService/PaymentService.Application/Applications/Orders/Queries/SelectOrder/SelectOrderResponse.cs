using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Orders.Queries.SelectOrder;

public record SelectOrderResponse : AbstractApiResponse<List<SelectOrderResponseEntity>>
{
    public override List<SelectOrderResponseEntity> Response { get; set; }
    public int TotalRecords { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

public class SelectOrderResponseEntity
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public short Status { get; set; }
    public string StatusName { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Currency { get; set; } = null!;
    public DateTime? PaidAt { get; set; }
    public List<OrderItemEntity> OrderItems { get; set; }
}

public class OrderItemEntity
{
    public Guid OrderItemId { get; set; }
    public Guid CourseId { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalPrice { get; set; }
}

