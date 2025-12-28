namespace Course.Application.DTOs.SyllabusDTO
{
	public record CreateFullSyllabusDto(
		Guid MajorId,
		string VersionLabel,
		DateOnly EffectiveFrom,
		DateOnly? EffectiveTo,
		List<FullSemesterDto> Semesters
	);

	public record FullSemesterDto(
		Guid SemesterId,
		int PositionIndex,
		List<FullSubjectDto> Subjects
	);

	public record FullSubjectDto(
		Guid SubjectId,
		short? Credit,
		bool IsMandatory,
		int PositionIndex
	);

}
