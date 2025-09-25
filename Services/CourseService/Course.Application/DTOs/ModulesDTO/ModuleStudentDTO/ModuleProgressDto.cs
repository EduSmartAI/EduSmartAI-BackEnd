namespace Course.Application.DTOs.ModulesDTO.ModuleStudentDTO
{
	public record ModuleProgressDto(
		int LessonsTotal,
		int LessonsCompleted,
		decimal PercentCompleted, // 0..100
		short Status,             // 0/1/2
		DateTime? StartedAt,
		DateTime? CompletedAt
	);
}
