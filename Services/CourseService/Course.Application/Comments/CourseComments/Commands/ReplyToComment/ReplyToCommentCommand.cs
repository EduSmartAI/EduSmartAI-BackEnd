using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.CourseComments.Commands.ReplyToComment
{
	public record ReplyToCommentCommand(Guid CourseId, Guid ParentCommentId, string Content) : ICommand<ReplyToCommentResponse>;

	public sealed record ReplyToCommentResponse : AbstractApiResponse<CourseCommentDetailsDto>
	{
		public override CourseCommentDetailsDto Response { get; set; } = default!;
	}
}
