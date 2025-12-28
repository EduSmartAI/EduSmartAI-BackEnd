namespace Course.Application.Subjects.Queries.GetSubjectDetails
{
	public class GetSubjectDetailHandler(ISubjectService _subjectService) : IQueryHandler<GetSubjectDetailQuery, GetSubjectDetailResponse>
	{
		public async Task<GetSubjectDetailResponse> Handle(GetSubjectDetailQuery request, CancellationToken cancellationToken)
		{
			return await _subjectService.GetSubjectDetailAsync(
				request.SubjectId,
				cancellationToken
			);
		}
	}
}
