using Course.Application.DTOs.SyllabusDTO.Subjects;

namespace Course.Application.Subjects.Queries.GetSubjectDetails
{
	public sealed record GetSubjectDetailQuery(
		Guid SubjectId
	) : IQuery<GetSubjectDetailResponse>;

	public sealed record GetSubjectDetailResponse : AbstractApiResponse<SubjectWithPrereqsDto?>
	{
		public override SubjectWithPrereqsDto? Response { get; set; } = default!;
	}
}
