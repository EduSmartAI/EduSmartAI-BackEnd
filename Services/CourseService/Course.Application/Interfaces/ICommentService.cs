using Course.Application.Comments.Commands.CreateComment;
using Course.Application.Comments.Commands.ReplyToComment;
using Course.Application.Comments.Queries.GetCourseComments;

namespace Course.Application.Interfaces
{
	public interface ICommentService
	{
		Task<CreateCommentResponse> CreateAsync(Guid courseId, string content, CancellationToken ct = default);
		Task<ReplyToCommentResponse> ReplyAsync(Guid courseId, Guid parentCommentId, string content, CancellationToken ct = default);
		Task<GetCourseCommentsResponse> GetCourseCommentsAsync(Guid courseId, int? page, int? size, CancellationToken ct = default);
	}
}
