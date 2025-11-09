namespace Course.Application.Comments.Commands.ReplyToComment
{
	public sealed class ReplyToCommentHandler(ICommentService _commentService)
	: ICommandHandler<ReplyToCommentCommand, ReplyToCommentResponse>
	{
		public async Task<ReplyToCommentResponse> Handle(ReplyToCommentCommand request, CancellationToken cancellationToken)
			=> await _commentService.ReplyAsync(request.CourseId, request.ParentCommentId, request.Content, cancellationToken);
	}
}
