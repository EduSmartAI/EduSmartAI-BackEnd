namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseDto(
		Guid CourseId,
		Guid TeacherId,
		Guid SubjectId,
		string SubjectCode,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,
		string? CourseImageUrl,
		int LearnerCount,
		int? DurationMinutes,
		decimal? DurationHours,
		short? Level,
		decimal Price,
		decimal? DealPrice,
		bool IsActive,
		DateTime CreatedAt,
		DateTime UpdatedAt,
		List<CourseTagDto> Tags
	);

}
