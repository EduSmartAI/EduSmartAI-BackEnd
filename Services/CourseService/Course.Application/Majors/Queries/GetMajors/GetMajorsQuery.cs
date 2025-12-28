using BaseService.Application.Common;
using Course.Application.DTOs.SyllabusDTO.Majors;

namespace Course.Application.Majors.Queries.GetMajors
{
	public sealed record GetMajorsQuery(
		int? Page,
		int? Size,
		string? Search
	) : IQuery<GetMajorsResponse>;

	public sealed record GetMajorsResponse : AbstractApiResponse<PagedResult<MajorDto>>
	{
		public override PagedResult<MajorDto> Response { get; set; } = default!;
	}
}
