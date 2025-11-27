using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;

public sealed record AiUpdateCourseStatusToSkipped(Guid UserId, string Email, Guid LearningPathId, string SubjectCode);

public sealed record AiUpdateCourseStatusToSkippedResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = string.Empty;
}