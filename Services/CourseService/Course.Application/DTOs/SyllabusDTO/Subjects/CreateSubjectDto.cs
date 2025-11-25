namespace Course.Application.DTOs.SyllabusDTO.Subjects
{
	public record CreateSubjectDto(
		string SubjectCode,
		string SubjectName,
		IReadOnlyList<Guid>? PrerequisiteSubjectIds
	);
}
