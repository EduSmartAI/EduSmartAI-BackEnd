namespace TeacherService.Application.DTOs
{
	public sealed record TeacherQualificationDto(
		Guid QualificationId,
		string? DegreeTitle,
		string? Institution,
		DateOnly? StartDate,
		DateOnly? EndDate,
		string? Description,
		string? CertificateUrl
	);
}
