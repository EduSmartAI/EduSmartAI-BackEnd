namespace Course.Application.DTOs.ModulesDTO.ModuleMaterialDetailDTO
{
	public record ModuleMaterialDetailDto(
		Guid MaterialId,
		string Title,
		string Description,
		string FileUrl,
		DateTime CreatedAt,
		DateTime UpdatedAt
		);
}
