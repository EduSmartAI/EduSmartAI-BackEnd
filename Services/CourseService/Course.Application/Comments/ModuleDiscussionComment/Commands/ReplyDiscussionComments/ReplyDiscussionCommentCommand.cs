using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.ModuleDiscussionComment.Commands.ReplyDiscussionComments
{
	public record ReplyDiscussionCommentCommand(Guid ModuleId, Guid ParentCommentId, string Content) : ICommand<ReplyDiscussionCommentResponse>;

	public sealed record ReplyDiscussionCommentResponse : AbstractApiResponse<bool> 
	{ 
		public override bool Response { get; set; } = default!;
	}
}
