using Course.Application.DTOs.SyllabusDTO.Subjects;

namespace Course.Application.Subjects.Commands.CreateSubject
{
	public record CreateSubjectCommand(CreateSubjectDto CreateSubjectDto) : ICommand<CreateSubjectResponse>;

	public record CreateSubjectResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	};
}
