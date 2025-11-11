namespace Course.Application.Comments.ModuleDiscussionComment.Commands.PostModuleDiscussionComments
{
	public class PostDiscussionCommentHandler(IModuleDiscussionCommentService _moduleDiscussionCommentService) : ICommandHandler<PostDiscussionCommentCommand, PostDiscussionCommentResponse>
	{
		public async Task<PostDiscussionCommentResponse> Handle(PostDiscussionCommentCommand request, CancellationToken cancellationToken)
		{
			return await _moduleDiscussionCommentService.PostAsync(request.ModuleId, request.Content, cancellationToken);
		}
	}
}
