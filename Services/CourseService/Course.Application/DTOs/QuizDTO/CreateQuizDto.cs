namespace Course.Application.DTOs.QuizDTO
{
	public record CreateQuizDto(
		CreateQuizSettingsDto QuizSettings,
		List<Questions> Questions
	);
}
