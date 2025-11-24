using Course.Application.DTOs.SyllabusDTO;

namespace Course.Application.Syllabus.Commands.AddSemester
{
	public record AddSemesterCommand(Guid SyllabusId, AddSemesterToSyllabusDto Dto) : ICommand<AddSemesterResponse>;

	public record AddSemesterResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}
}
