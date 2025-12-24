using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;

/// <summary>
/// Set/replace the learner's active learning goal.
/// </summary>
public sealed record AiSetLearningGoal(Guid UserId, string Email, Guid LearningGoalId);

public sealed record AiSetLearningGoalResponse : AbstractApiResponse<bool>
{
    public override bool Response { get; set; }
}


