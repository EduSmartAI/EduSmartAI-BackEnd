using Course.Application.DTOs.SyllabusDTO.Semester;

namespace Course.Application.Semesters.Queries.GetSemesterDetails
{
	public sealed record GetSemesterDetailQuery(
		Guid SemesterId
	) : IQuery<GetSemesterDetailResponse>;

	public sealed record GetSemesterDetailResponse : AbstractApiResponse<SemesterDto>
	{
		public override SemesterDto Response { get; set; } = default!;
	}
}
