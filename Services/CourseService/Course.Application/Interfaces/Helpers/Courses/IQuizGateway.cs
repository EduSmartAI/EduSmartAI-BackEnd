using Course.Application.DTOs.QuizDTO;

namespace Course.Application.Interfaces.Helpers.Courses
{
	public interface IQuizGateway
	{
		Task<QuizOutDto?> FetchQuizForLectureAsync(Guid quizId, CancellationToken ct);
		Task<QuizOutDto?> FetchQuizForStudentAsync(Guid quizId, CancellationToken ct);
	}
}
