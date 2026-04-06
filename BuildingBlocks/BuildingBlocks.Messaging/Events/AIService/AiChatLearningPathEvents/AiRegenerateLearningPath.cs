using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;

/// <summary>
/// Chat-triggered regenerate learning path request (StudentService handles the regeneration).
/// </summary>
public sealed record AiRegenerateLearningPath(Guid UserId, string Email);

public sealed record AiRegenerateLearningPathResponse : AbstractApiResponse<Guid?>
{
    public override Guid? Response { get; set; }
}


