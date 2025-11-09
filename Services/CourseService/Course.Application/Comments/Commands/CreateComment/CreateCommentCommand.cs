using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.Commands.CreateComment
{
	public record CreateCommentCommand(Guid CourseId, string Content) : ICommand<CreateCommentResponse>;

	public sealed record CreateCommentResponse : AbstractApiResponse<CommentDto>
	{
		public override CommentDto Response { get; set; } = default!;
	}
}
