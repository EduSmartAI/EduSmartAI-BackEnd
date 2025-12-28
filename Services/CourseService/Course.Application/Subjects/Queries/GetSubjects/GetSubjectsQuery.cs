using BaseService.Application.Common;
using Course.Application.DTOs.SyllabusDTO.Subjects;

namespace Course.Application.Subjects.Queries.GetSubjects
{
	public sealed record GetSubjectsQuery(
		int? Page,
		int? Size,
		string? Search
	) : IQuery<GetSubjectsResponse>;

	public sealed record GetSubjectsResponse : AbstractApiResponse<PagedResult<SubjectWithPrereqsDto>>
	{
		public override PagedResult<SubjectWithPrereqsDto> Response { get; set; } = default!;
	}
}
