namespace Course.Application.DTOs.QuizDTO
{
	public record QuizOutDto(
		QuizSettingsOutDto QuizSettings,
		List<QuizQuestionOutDto> Questions
	);
}
