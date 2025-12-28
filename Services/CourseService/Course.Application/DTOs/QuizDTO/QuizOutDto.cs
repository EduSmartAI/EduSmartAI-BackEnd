namespace Course.Application.DTOs.QuizDTO
{
	public record QuizOutDto(
		Guid QuizId,
		QuizSettingsOutDto QuizSettings,
		List<QuizQuestionOutDto> Questions
	);
}
