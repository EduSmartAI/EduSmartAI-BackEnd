using Course.Application.DTOs.SyllabusDTO;

namespace Course.Application.Syllabus.Queries
{
	public record GetFullSyllabusQuery(string VersionLabel) : IQuery<GetFullSyllabusResponse>;

	public record GetFullSyllabusResponse : AbstractApiResponse<SyllabusFullDto>
	{
		public override SyllabusFullDto Response { get; set; } = default!;
	}
}
