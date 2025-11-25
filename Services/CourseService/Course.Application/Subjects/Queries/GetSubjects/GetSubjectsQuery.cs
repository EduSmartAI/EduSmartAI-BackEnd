using BaseService.Application.Common;
using Course.Application.DTOs.SyllabusDTO.Subjects;

namespace Course.Application.Subjects.Queries.GetSubjects
{
	public sealed record GetSubjectsQuery(
		int? Page,
		int? Size,
		string? Search
	) : IQuery<GetSubjectsResponse>;

	public sealed record GetSubjectsResponse : AbstractApiResponse<PagedResult<SubjectDto>>
	{
		public override PagedResult<SubjectDto> Response { get; set; } = default!;
	}
}
