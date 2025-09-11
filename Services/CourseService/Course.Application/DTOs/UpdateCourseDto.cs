namespace Course.Application.DTOs
{
	public record UpdateCourseDto(
		Guid TeacherId,
		Guid SubjectId,
		string Title,
		string? Description,
		decimal Price,
		decimal? DealPrice,
		short? Level,
		int? DurationMinutes,
		bool IsActive,
		List<UpdateModuleDto> Modules
	);
}
