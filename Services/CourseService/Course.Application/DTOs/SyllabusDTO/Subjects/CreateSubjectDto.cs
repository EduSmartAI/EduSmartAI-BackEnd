namespace Course.Application.DTOs.SyllabusDTO.Subjects
{
	public record CreateSubjectDto(
		string SubjectCode,
		string SubjectName,
		string SubjectDescription,
		IReadOnlyList<Guid>? PrerequisiteSubjectIds
	);
}
