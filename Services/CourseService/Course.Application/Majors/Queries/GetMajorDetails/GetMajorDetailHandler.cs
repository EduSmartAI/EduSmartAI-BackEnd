namespace Course.Application.Majors.Queries.GetMajorDetails
{
	public class GetMajorDetailHandler(IMajorService _majorService) : IQueryHandler<GetMajorDetailQuery, GetMajorDetailResponse>
	{
		public async Task<GetMajorDetailResponse> Handle(GetMajorDetailQuery request, CancellationToken cancellationToken)
		{
			return await _majorService.GetMajorDetailAsync(
				request.MajorId,
				cancellationToken
			);
		}
	}
}
