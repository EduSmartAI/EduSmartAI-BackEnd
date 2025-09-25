namespace Course.Application.DTOs.CoursesDTO.CourseStudentDTO
{
	public sealed record CourseProgressSnap(
		int LessonsTotal,
		int LessonsCompleted,
		decimal PercentCompleted,
		short Status, 
		DateTime? StartedAt, 
		DateTime? CompletedAt
	);
}
