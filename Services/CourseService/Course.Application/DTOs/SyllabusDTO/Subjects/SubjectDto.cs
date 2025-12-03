namespace Course.Application.DTOs.SyllabusDTO.Subjects
{
	public sealed record SubjectDto(
		Guid SubjectId,
		string SubjectCode,
		string SubjectName,
		string? SubjectDescription = null
	);
}
