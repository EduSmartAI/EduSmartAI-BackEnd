using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.QuizService.QuizzesCourseInsertEvents
{
	public record CreateQuizPayload(
		CreateQuizSettingsPayload QuizSettings,
		List<CreateQuizQuestionPayload> Questions
	);

	public record CreateQuizSettingsPayload(
		int DurationMinutes,
		int PassingScorePercentage,
		bool ShuffleQuestions,
		bool ShowResultsImmediately,
		bool AllowRetake
	);

	public record CreateQuizOptionPayload(
		string Text,
		bool IsCorrect
	);

	public record CreateQuizQuestionPayload(
		QuestionType QuestionType,
		string QuestionText,
		List<CreateQuizOptionPayload> Options,
		string? Explanation
	);
}
