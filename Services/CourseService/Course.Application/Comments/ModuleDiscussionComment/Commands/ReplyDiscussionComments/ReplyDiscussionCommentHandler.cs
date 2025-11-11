

namespace Course.Application.Comments.ModuleDiscussionComment.Commands.ReplyDiscussionComments
{
	public class ReplyDiscussionCommentHandler(IModuleDiscussionCommentService _moduleDiscussionCommentService) : ICommandHandler<ReplyDiscussionCommentCommand, ReplyDiscussionCommentResponse>
	{
		public async Task<ReplyDiscussionCommentResponse> Handle(ReplyDiscussionCommentCommand request, CancellationToken cancellationToken)
		{
			return await _moduleDiscussionCommentService.ReplyAsync(request.ModuleId, request.ParentCommentId, request.Content, cancellationToken);
		}
	}
}
