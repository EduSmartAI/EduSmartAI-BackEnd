namespace Course.Application.DTOs.SyllabusDTO.Majors
{
	public sealed record MajorDetailDto(
		Guid MajorId,
		string MajorCode,
		string MajorName,
		string? Description,
		bool IsActive,
		DateTimeOffset CreatedAt,
		DateTimeOffset UpdatedAt,
		string? CreatedBy,
		string? UpdatedBy
	);
}
