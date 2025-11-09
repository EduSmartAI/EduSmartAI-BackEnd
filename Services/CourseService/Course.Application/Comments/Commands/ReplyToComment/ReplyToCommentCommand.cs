using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.Commands.ReplyToComment
{
	public record ReplyToCommentCommand(Guid CourseId, Guid ParentCommentId, string Content) : ICommand<ReplyToCommentResponse>;

	public sealed record ReplyToCommentResponse : AbstractApiResponse<CommentDto>
	{
		public override CommentDto Response { get; set; } = default!;
	}
}
