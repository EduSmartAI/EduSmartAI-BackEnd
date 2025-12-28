using Course.Application.DTOs.QuizDTO;

namespace Course.Application.DTOs.LessonsDTO
{
	public record CreateLessonDto(
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive,
		CreateQuizDto? LessonQuiz
	);
}
