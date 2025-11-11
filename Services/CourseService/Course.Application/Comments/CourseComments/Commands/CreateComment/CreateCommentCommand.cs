using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.CourseComments.Commands.CreateComment
{
	public record CreateCommentCommand(Guid CourseId, string Content) : ICommand<CreateCommentResponse>;

	public sealed record CreateCommentResponse : AbstractApiResponse<CourseCommentDto>
	{
		public override CourseCommentDto Response { get; set; } = default!;
	}
}
