namespace TeacherService.Application.DTOs
{
	public sealed record TeacherDetailDto(
		Guid TeacherId,
		string DisplayName,
		string? FirstName,
		string? LastName,
		string? Bio,
		string? ProfilePictureUrl,
		List<TeacherCertificateDto> Certificates,
		List<TeacherExperienceDto> Experiences,
		List<TeacherQualificationDto> Qualifications
	);
}
