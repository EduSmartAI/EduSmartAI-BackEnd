namespace Course.Application.Comments.Queries.GetCourseComments
{
	public sealed class GetCourseCommentsHandler(ICommentService _commentService) : IQueryHandler<GetCourseCommentsQuery, GetCourseCommentsResponse>
	{
		public async Task<GetCourseCommentsResponse> Handle(GetCourseCommentsQuery request, CancellationToken cancellationToken)
			=> await _commentService.GetCourseCommentsAsync(request.CourseId, request.Threaded, request.Page, request.Size, cancellationToken);
	}
}
