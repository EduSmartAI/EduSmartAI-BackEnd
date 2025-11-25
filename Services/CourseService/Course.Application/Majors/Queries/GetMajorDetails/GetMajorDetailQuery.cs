using Course.Application.DTOs.SyllabusDTO.Majors;

namespace Course.Application.Majors.Queries.GetMajorDetails
{
	public sealed record GetMajorDetailQuery(
		Guid MajorId
	) : IQuery<GetMajorDetailResponse>;

	public sealed record GetMajorDetailResponse : AbstractApiResponse<MajorDto?>
	{
		public override MajorDto? Response { get; set; } = default!;
	}
}
