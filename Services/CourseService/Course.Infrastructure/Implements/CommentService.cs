using Course.Application.Comments.Commands.CreateComment;
using Course.Application.Comments.Commands.ReplyToComment;
using Course.Application.Comments.Queries.GetCourseComments;

namespace Course.Infrastructure.Implements
{
	public sealed class CommentService(
		IDatabase cache,
		IIdentityService identity,
		IUnitOfWork uow,
		ICommandRepository<CourseComment> commentCmd,
		ICommandRepository<CourseStudentEnrollment> enrollCmd
	) : ICommentService
	{
		public Task<CreateCommentResponse> CreateAsync(Guid courseId, string content, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}

		public Task<GetCourseCommentsResponse> GetCourseCommentsAsync(Guid courseId, bool threaded, int? page, int? size, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}

		public Task<ReplyToCommentResponse> ReplyAsync(Guid courseId, Guid parentCommentId, string content, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}
	}
}
