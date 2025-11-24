namespace Course.Application.DTOs.SyllabusDTO.Subjects
{
	public record AddSubjectToSyllabusDto(
		Guid SubjectId,
		short? Credit,
		bool IsMandatory,
		int PositionIndex
	);
}
