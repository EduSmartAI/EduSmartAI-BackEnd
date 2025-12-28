namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseRatingDto(
		Guid RatingId,
		Guid UserId,
		short Rating,
		DateTime CreatedAt
	);
}
