using Course.Application.DTOs.QuizDTO;

namespace Course.Application.Interfaces.Helpers.Courses
{
	public interface IQuizGateway
	{
		Task<QuizOutDto?> FetchQuizAsync(Guid quizId, CancellationToken ct);
	}
}
