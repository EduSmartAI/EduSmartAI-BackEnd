using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;

namespace AiService.Application.DTOs
{
	public record AiEvaluationDto(
		int Score100,
		string Summary,
		IReadOnlyList<string> Strengths,
		IReadOnlyList<string> Improvements,
		IReadOnlyList<RecommendedAction> Actions,
		IReadOnlyList<SkillGap> SkillGaps,
		double Confidence,
		string Model,
		string RubricVersion,
		DateTime CreatedAtUtc
	);
}
