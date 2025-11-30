using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.PaymentService;

public class PaymentSucceededEvent
{
    public List<Guid> CourseIds { get; set; } = null!;
    
    public Guid UserId { get; set; }
    
    public string Email { get; set; } = null!;
}

public record PaymentSucceededEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}