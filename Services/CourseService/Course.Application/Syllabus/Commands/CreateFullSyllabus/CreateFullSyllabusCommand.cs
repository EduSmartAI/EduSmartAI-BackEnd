using Course.Application.DTOs.SyllabusDTO;

namespace Course.Application.Syllabus.Commands.CreateFullSyllabus
{
	public record CreateFullSyllabusCommand(CreateFullSyllabusDto CreateFullSyllabusDto) : ICommand<CreateFullSyllabusResponse>;

	public record CreateFullSyllabusResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}

}
