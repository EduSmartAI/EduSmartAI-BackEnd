namespace Course.Application.DTOs.ModulesDTO
{
	public record CreateModuleObjectiveDto(string Content, int PositionIndex = 0, bool IsActive = true);
}
