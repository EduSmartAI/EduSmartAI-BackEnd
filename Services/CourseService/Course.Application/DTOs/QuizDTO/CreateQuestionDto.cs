namespace Course.Application.DTOs.QuizDTO
{
	public record Questions(
		int QuestionType,
		string QuestionText,
		List<Answers> Options,
		string? Explanation
	);
}
