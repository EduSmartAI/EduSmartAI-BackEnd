using Course.Application.DTOs.SyllabusDTO.Subjects;

namespace Course.Application.Subjects.Commands.AddSubjectToSyllabus
{
	public record AddSubjectCommand(Guid SyllabusId, Guid SemesterId, AddSubjectToSyllabusDto Dto) : ICommand<AddSubjectResponse>;

	public record AddSubjectResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}
}
