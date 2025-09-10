namespace Course.Application.DTOs
{
	public record CreateCourseDto(
		Guid TeacherId,
		Guid SubjectId,
		string Title,
		string? Description,
		decimal Price,
		decimal? DealPrice,
		short? Level,
		int? DurationMinutes,
		bool IsActive,
		List<CreateModuleDto> Modules
	);
}
