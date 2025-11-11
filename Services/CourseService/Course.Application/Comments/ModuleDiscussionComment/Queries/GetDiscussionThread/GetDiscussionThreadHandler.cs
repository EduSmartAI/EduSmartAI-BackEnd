namespace Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread
{
	public class GetDiscussionThreadHandler(IModuleDiscussionCommentService _moduleDiscussionCommentService) : IQueryHandler<GetDiscussionThreadQuery, GetDiscussionThreadResponse>
	{
		public Task<GetDiscussionThreadResponse> Handle(GetDiscussionThreadQuery request, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
