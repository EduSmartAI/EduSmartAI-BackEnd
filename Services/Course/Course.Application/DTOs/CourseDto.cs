namespace Course.Application.DTOs
{
	public record CourseDto(
		Guid CourseId,
		Guid TeacherId,
		Guid SubjectId,
		string Title,
		string? Description,
		int? DurationMinutes,
		short? Level,
		decimal Price,
		decimal? DealPrice,
		bool IsActive,
		DateTime CreatedAt,
		DateTime UpdatedAt
	);
}
