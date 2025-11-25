using Course.Application.DTOs.SyllabusDTO;

namespace Course.Application.Syllabus.Commands.CloneFoundationSyllabus
{
	public record CloneFoundationSyllabusCommand(CloneFoundationSyllabusDto CloneFoundationSyllabusDto) : ICommand<CloneFoundationSyllabusResponse>;

	public record CloneFoundationSyllabusResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}

}
