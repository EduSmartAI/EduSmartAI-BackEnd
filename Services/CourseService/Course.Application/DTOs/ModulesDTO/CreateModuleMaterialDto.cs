namespace Course.Application.DTOs.ModulesDTO
{
	public record CreateModuleMaterialDto(
		string Title,
		string? Description,
		string? FileUrl,
		bool IsActive = true
	);
}
