using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;

/// <summary>
/// Get the learner's current active learning goal (if any).
/// </summary>
public sealed record AiGetCurrentLearningGoal(Guid UserId);

public sealed record AiGetCurrentLearningGoalResponse : AbstractApiResponse<AiCurrentLearningGoalDto?>
{
    public override AiCurrentLearningGoalDto? Response { get; set; }
}

public sealed record AiCurrentLearningGoalDto
{
    public Guid LearningGoalId { get; set; }
    public string LearningGoalName { get; set; } = string.Empty;
    public short LearningGoalType { get; set; }
}


