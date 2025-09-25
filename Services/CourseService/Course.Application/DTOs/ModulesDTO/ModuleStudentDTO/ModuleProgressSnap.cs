namespace Course.Application.DTOs.ModulesDTO.ModuleStudentDTO
{
	public sealed record ModuleProgressSnap(
		Guid ModuleId, 
		int LessonsTotal,
		int LessonsCompleted,
		decimal PercentCompleted,
		short Status, 
		DateTime? StartedAt,
		DateTime? CompletedAt
	);
}
