namespace Course.Application.Comments.ModuleDiscussionComment.Commands.PostModuleDiscussionComments
{
	public record PostDiscussionCommentCommand(Guid ModuleId, string Content) : ICommand<PostDiscussionCommentResponse>;

	public sealed record PostDiscussionCommentResponse : AbstractApiResponse<bool> 
	{ 
		public override bool Response { get; set; } = default!; 
	}
}
