using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;

public sealed record GetAllLearningPath(Guid UserId);

public sealed record GetAllLearningPathResponse : AbstractApiResponse<List<AiLearningPathSummaryDto>>
{
    public override List<AiLearningPathSummaryDto> Response { get; set; } = new();
}

public sealed record AiLearningPathSummaryDto
{
    public Guid PathId { get; set; }
    public string PathName { get; set; } = string.Empty;
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}