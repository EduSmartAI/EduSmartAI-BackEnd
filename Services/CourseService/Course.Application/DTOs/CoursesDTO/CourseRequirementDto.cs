namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseRequirementDto(Guid RequirementId, string Content, int PositionIndex, bool IsActive);
}
