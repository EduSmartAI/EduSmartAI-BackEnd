namespace Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread
{
	public class GetDiscussionThreadHandler(IModuleDiscussionCommentService _moduleDiscussionCommentService) : IQueryHandler<GetDiscussionThreadQuery, GetDiscussionThreadResponse>
	{
		public async Task<GetDiscussionThreadResponse> Handle(GetDiscussionThreadQuery request, CancellationToken cancellationToken)
		{
			return await _moduleDiscussionCommentService.GetThreadAsync(request.ModuleId, request.Page, request.Size, cancellationToken);
		}
	}
}
