namespace Course.Application.DTOs.SyllabusDTO
{
	public record SyllabusFullDto(
		Guid SyllabusId,
		Guid MajorId,
		string MajorCode,
		string VersionLabel,
		DateOnly EffectiveFrom,
		DateOnly? EffectiveTo,
		List<SemesterWithSubjectsDto> Semesters
	);

	public record SemesterWithSubjectsDto(
		Guid SemesterId,
		string SemesterName,
		int PositionIndex,
		List<SubjectDetailDto> Subjects
	);

	public record SubjectDetailDto(
		Guid SubjectId,
		string SubjectCode,
		string SubjectName,
		short? Credit,
		bool IsMandatory,
		int PositionIndex,
		List<SubjectPrerequisiteDto> Prerequisites
	);

	public record SubjectPrerequisiteDto(
		Guid SubjectId,
		string SubjectCode,
		string SubjectName
	);

}
