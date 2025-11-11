namespace Course.Application.Comments.CourseComments.Commands.CreateComment
{
	public sealed class CreateCommentHandler(ICommentService _commentService): ICommandHandler<CreateCommentCommand, CreateCommentResponse>
	{
		public async Task<CreateCommentResponse> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
			=> await _commentService.CreateAsync(request.CourseId, request.Content, cancellationToken);
	}
}
