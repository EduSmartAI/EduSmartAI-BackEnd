namespace Course.Application.DTOs.ModulesDTO
{
	public record UpdateModuleObjectiveDto(Guid? ObjectiveId, string Content, int PositionIndex, bool IsActive);
}
