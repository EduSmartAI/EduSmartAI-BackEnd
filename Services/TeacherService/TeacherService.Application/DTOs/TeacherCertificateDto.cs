namespace TeacherService.Application.DTOs
{
	public sealed record TeacherCertificateDto(
		Guid CertificateId,
		string CertName,
		string? Issuer,
		DateOnly? IssuedDate,
		DateOnly? ExpireDate,
		string? CertUrl
	);
}
