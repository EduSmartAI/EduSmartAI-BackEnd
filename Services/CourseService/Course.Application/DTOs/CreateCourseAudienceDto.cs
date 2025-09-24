namespace Course.Application.DTOs
{
	public record CreateCourseAudienceDto(
		string Content,
		int PositionIndex = 0,
		bool IsActive = true
	);
}
