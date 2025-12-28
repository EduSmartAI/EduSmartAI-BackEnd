namespace Course.Application.DTOs.SyllabusDTO.Majors
{
	public sealed record MajorSummaryDto(
		Guid MajorId,
		string MajorCode,
		string MajorName,
		bool IsActive,
		DateTimeOffset CreatedAt,
		DateTimeOffset UpdatedAt,
		string? CreatedBy,
		string? UpdatedBy
	);
}
