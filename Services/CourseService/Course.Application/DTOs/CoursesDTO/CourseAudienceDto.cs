namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseAudienceDto(
		Guid AudienceId,
		string Content,
		int PositionIndex,
		bool IsActive
	);
}
