namespace Course.Application.DTOs.CoursesDTO.CourseStudentDTO
{
	public record CourseProgressDto(
		int LessonsTotal,
		int LessonsCompleted,
		decimal PercentCompleted, // 0..100; chỉ CORE
		short Status,             // 0/1/2
		DateTime? StartedAt,
		DateTime? CompletedAt
	);
}
