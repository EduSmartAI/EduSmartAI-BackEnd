namespace TeacherService.Application.DTOs
{
	public sealed record UpdateTeacherProfileRequest(
		string DisplayName,
		string? FirstName,
		string? LastName,
		string? Bio,
		string? ProfilePictureUrl
	);
}
