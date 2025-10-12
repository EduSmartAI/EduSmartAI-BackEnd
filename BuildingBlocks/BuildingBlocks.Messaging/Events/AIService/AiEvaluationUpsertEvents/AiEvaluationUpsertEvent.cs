using BaseService.Common.ApiEntities;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents
{
	public record AiEvaluationUpsertEvent
	(
		Guid AttemptId,
		Guid UserId,
		Guid CourseId,
		QuizScope Scope,
		Guid ScopeId,
		Guid QuizId,
		short Score100,
		string Summary,
		IReadOnlyList<string> Strengths,
		IReadOnlyList<string> Improvements,
		IReadOnlyList<RecommendedAction> Actions,
		IReadOnlyList<SkillGap> SkillGaps,
		decimal Confidence,
		string Model,
		string RubricVersion,
		DateTime CreatedAtUtc
	);

	public sealed record SkillGap(string SkillTag, int Level, string Evidence);
	public sealed record RecommendedAction(string Title, string Kind, string? TargetUrl);
}
