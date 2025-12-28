using Course.Application.DTOs.SyllabusDTO;

namespace Course.Application.Syllabus.Queries.GetFullSyllabus
{
	public record GetFullSyllabusQuery(string VersionLabel, string MajorCode) : IQuery<GetFullSyllabusResponse>;

	public record GetFullSyllabusResponse : AbstractApiResponse<SyllabusFullDto>
	{
		public override SyllabusFullDto Response { get; set; } = default!;
	}
}
