namespace Course.Application.DTOs.ModulesDTO
{
	public record CreateModuleDiscussionDto(
		string Title,
		string? Description,
		string? DiscussionQuestion,
		bool IsActive = true
	);
}
