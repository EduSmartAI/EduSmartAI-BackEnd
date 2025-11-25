namespace Course.Application.DTOs.SyllabusDTO.Semester
{
	public sealed record SemesterDto(
		Guid SemesterId,
		string SemesterCode,
		string SemesterName,
		short SemesterNumber
	);
}
