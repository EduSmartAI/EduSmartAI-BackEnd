namespace Course.Application.DTOs.ModulesDTO.ModuleDiscussionDTO
{
	public record ModuleDiscussionDetailDto(
		Guid DiscussionId,
		string Title,
		string Description,
		string DiscussionQuestion,
		DateTime CreatedAt,
		DateTime? UpdatedAt
	);
}
