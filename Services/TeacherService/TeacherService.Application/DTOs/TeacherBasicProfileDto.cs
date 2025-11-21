namespace TeacherService.Application.DTOs
{
	public sealed record TeacherBasicProfileDto(
		Guid TeacherId,
		string DisplayName,
		string? FirstName,
		string? LastName,
		string? ProfilePictureUrl,
		string? Bio
	);
}
