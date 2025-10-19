using Course.Application.DTOs.QuizDTO;

namespace Course.Application.DTOs.LessonsDTO.LessonStudentDTO
{
	public record StudentLessonDetailDto(
		Guid LessonId,
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive,
		bool IsCompleted,   // NEW: tick bài
		int LastPositionSec,
		bool CanAttempt,
		Guid? StudentQuizId,
		QuizOutDto? LessonQuiz = null
	);
}
