using BaseService.Application.Common;
using Course.Application.DTOs.SyllabusDTO.Semester;

namespace Course.Application.Semesters.Queries.GetSemesters
{
	public sealed record GetSemestersQuery(
		int? Page,
		int? Size,
		string? Search
	) : IQuery<GetSemestersResponse>;

	public sealed record GetSemestersResponse : AbstractApiResponse<PagedResult<SemesterDto>>
	{
		public override PagedResult<SemesterDto> Response { get; set; } = default!;
	}
}
