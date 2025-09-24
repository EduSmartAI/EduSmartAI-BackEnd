namespace Course.Application.DTOs.QuizDTO
{
	public record QuizQuestionOutDto(
		Guid QuestionId,
		string QuestionText,
		string? Explanation,
		short QuestionType,
		List<QuizAnswerOutDto> Answers
	);
}
