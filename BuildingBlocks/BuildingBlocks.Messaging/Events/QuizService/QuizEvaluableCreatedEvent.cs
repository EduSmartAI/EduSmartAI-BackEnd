using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.QuizService
{
	public record QuizEvaluableCreatedEvent(
		Guid EventId,
		Guid AttemptId,
		Guid QuizId,
		QuizScope Scope,
		Guid ScopeId,
		Guid CourseId,
		Guid UserId,
		int TotalQuestions,
		int TotalCorrectAnswers,
		short Score100Raw,
		IReadOnlyList<QuestionResult> Questions,
		DateTime OccurredAtUtc
	);

	public record QuestionResult(
		Guid QuestionId, 
		string QuestionText, 
		short QuestionType, 
		string? Explanation,
		IReadOnlyList<AnswerResult> Answers
	);

	public record AnswerResult(Guid? AnswerId, string? AnswerText, bool IsCorrectAnswer, bool SelectedByStudent);
}
