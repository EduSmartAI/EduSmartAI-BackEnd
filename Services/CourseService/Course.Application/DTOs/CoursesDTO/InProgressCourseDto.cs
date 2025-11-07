namespace Course.Application.DTOs.CoursesDTO
{
	public sealed record InProgressCourseDto
	(
		Guid CourseId,
		string Title,
		string? ShortDescription,
		string? CourseImageUrl,
		decimal? DurationHours,
		DateTime StartedAt
	);
}
