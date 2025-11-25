namespace Course.Application.Comments.CourseComments.Commands.DeleteComment
{
	public class DeleteCommentHandler(ICommentService _commentService) : ICommandHandler<DeleteCommentCommand, DeleteCommentResponse>
	{
		public async Task<DeleteCommentResponse> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
		{
			return await _commentService.DeleteCommentAsync(request, cancellationToken);
		}
	}
}
