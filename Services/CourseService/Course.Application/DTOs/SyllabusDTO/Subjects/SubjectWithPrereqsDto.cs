namespace Course.Application.DTOs.SyllabusDTO.Subjects
{
	public sealed record SubjectWithPrereqsDto(
		Guid SubjectId,
		string SubjectCode,
		string SubjectName,
		IReadOnlyList<SubjectPrereqDto> Prerequisites,
		string? SubjectDescription = null
	);

	public sealed record SubjectPrereqDto(
		Guid SubjectId,
		string SubjectCode,
		string SubjectName
	);
}
