using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent
{
    public sealed record InternalMajorEvent(
        Guid LearningPathId,
        short StudentLevel,
        string LimitTime,
        string CurrentUserEmail,
        IReadOnlyList<InternalMajorItem> Majors,
        Guid SemesterId);
        
    public sealed record InternalMajorItem(
        string MajorCode,
        string Reason
    );
    
    public record InternalMajorEventResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
