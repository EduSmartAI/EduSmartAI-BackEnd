namespace Course.Application.DTOs.QuizDTO
{
	public record CreateQuizSettingsDto(
		int DurationMinutes,
		int PassingScorePercentage,
		bool ShuffleQuestions,
		bool ShowResultsImmediately,
		bool AllowRetake
	);
}
