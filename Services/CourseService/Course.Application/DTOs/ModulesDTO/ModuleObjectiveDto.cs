namespace Course.Application.DTOs.ModulesDTO
{
	public record ModuleObjectiveDto(Guid ObjectiveId, string Content, int PositionIndex, bool IsActive);
}
