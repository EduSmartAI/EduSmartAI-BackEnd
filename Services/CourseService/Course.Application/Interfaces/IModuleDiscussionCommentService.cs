using Course.Application.Comments.ModuleDiscussionComment.Commands.PostModuleDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Commands.ReplyDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread;

namespace Course.Application.Interfaces
{
	public interface IModuleDiscussionCommentService
	{
		Task<PostDiscussionCommentResponse> PostAsync(Guid moduleId, string content, CancellationToken ct = default);
		Task<ReplyDiscussionCommentResponse> ReplyAsync(Guid moduleId, Guid parentCommentId, string content, CancellationToken ct = default);
		Task<GetDiscussionThreadResponse> GetThreadAsync(Guid moduleId, int? page, int? size, CancellationToken ct = default);
	}
}
