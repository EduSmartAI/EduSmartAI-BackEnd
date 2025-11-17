namespace TeacherService.Application.DTOs
{
	public sealed record TeacherExperienceDto(
		Guid ExperienceId,
		string? RoleTitle,
		string? Organization,
		DateOnly? StartDate,
		DateOnly? EndDate,
		bool IsCurrent,
		string? Description
	);
}
