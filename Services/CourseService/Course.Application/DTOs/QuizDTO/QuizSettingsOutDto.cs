namespace Course.Application.DTOs.QuizDTO
{
	public record QuizSettingsOutDto(
		int DurationMinutes,
		int PassingScorePercentage,
		bool ShuffleQuestions,
		bool ShowResultsImmediately,
		bool AllowRetake
	);
}
