namespace Course.Application.Syllabus.Commands.UpdateSyllabusSubjects
{
	public record UpdateSyllabusSubjectsCommand(Guid SyllabusId, List<UpdateSemesterSubjectsDto> Semesters) : ICommand<UpdateSyllabusSubjectsResponse>;

	public record UpdateSyllabusSubjectsResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}

	public record UpdateSemesterSubjectsDto(
		Guid SemesterId,
		List<UpdateSubjectDto> Subjects
	);

	public record UpdateSubjectDto(
		Guid SubjectId,
		short? Credit,
		bool IsMandatory
	);

}
