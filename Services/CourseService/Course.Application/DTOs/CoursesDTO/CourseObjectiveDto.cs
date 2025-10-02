namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseObjectiveDto(Guid ObjectiveId, string Content, int PositionIndex, bool IsActive);
}
