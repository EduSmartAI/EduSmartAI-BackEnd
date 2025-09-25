namespace Course.Application.DTOs.LessonsDTO.LessonStudentDTO
{
	public sealed record LessonProgressSnap(Guid LessonId, short Status, int LastPositionSec, DateTime? CompletedAt);
}
