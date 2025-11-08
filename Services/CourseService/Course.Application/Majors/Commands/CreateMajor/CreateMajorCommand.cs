using Course.Application.DTOs.SyllabusDTO.Majors;

namespace Course.Application.Majors.Commands.CreateMajor
{
	public record CreateMajorCommand(CreateMajorDto CreateMajorDto) : ICommand<CreateMajorResponse>;

	public record CreateMajorResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	};
}
