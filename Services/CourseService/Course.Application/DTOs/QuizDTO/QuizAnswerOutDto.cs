namespace Course.Application.DTOs.QuizDTO
{
	public record QuizAnswerOutDto(
		Guid AnswerId,
		string AnswerText,
		bool? IsCorrect
	);
}
