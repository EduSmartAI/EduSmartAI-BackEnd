namespace Course.Application.Comments.CourseComments.Commands.DeleteComment
{
	public record DeleteCommentCommand(Guid courseId, Guid CommentId) : ICommand<DeleteCommentResponse>;

	public record DeleteCommentResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}
}
