using Course.Application.DTOs.SyllabusDTO;

namespace Course.Application.Syllabus.Commands.CloneCascadeSyllabus
{
	public record CloneCascadeSyllabusCommand(CloneCascadeSyllabusDto CloneCascadeSyllabusDto) : ICommand<CloneCascadeSyllabusResponse>;

	public record CloneCascadeSyllabusResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}

}
