using Course.Application.Comments.ModuleDiscussionComment.Commands.PostModuleDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Commands.ReplyDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread;

namespace Course.Infrastructure.Implements
{
	public class ModuleDiscussionCommentService(
		IIdentityService _identity,
		IUnitOfWork _uow,
		ICommandRepository<ModuleDiscussionComment> _moduleDiscussionCommentCommand,
		ICommandRepository<ModuleDiscussion> _moduleDiscussionCommand,
		ICourseCache _cache
	) : IModuleDiscussionCommentService
	{
		public Task<GetDiscussionThreadResponse> GetThreadAsync(Guid moduleId, int? page, int? size, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Post Discussion Comment
		/// </summary>
		/// <param name="moduleId"></param>
		/// <param name="content"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<PostDiscussionCommentResponse> PostAsync(Guid moduleId, string content, CancellationToken ct = default)
		{
			var response = new PostDiscussionCommentResponse { Success = false };

			var user = _identity.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var discussion = await _moduleDiscussionCommand.FirstOrDefaultAsync(x => x.ModuleId == moduleId && x.IsActive, ct);

			if (discussion == null)
			{
				response.SetMessage(MessageId.E00000, "Discussion not found for the specified module.");
				return response;
			}

			var entity = new ModuleDiscussionComment
			{
				DiscussionId = discussion.DiscussionId,
				UserId = user.UserId,
				UserDisplayName = user.FullName,
				Content = content,
				ParentCommentId = null
			};

			await _moduleDiscussionCommentCommand.AddAsync(entity, user.Email);
			await _uow.SaveChangesAsync(user.Email, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Tạo bình luận");

			return response;
		}

		/// <summary>
		/// Reply Discussion Comment
		/// </summary>
		/// <param name="moduleId"></param>
		/// <param name="parentCommentId"></param>
		/// <param name="content"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<ReplyDiscussionCommentResponse> ReplyAsync(Guid moduleId, Guid parentCommentId, string content, CancellationToken ct = default)
		{
			var response = new ReplyDiscussionCommentResponse { Success = false };
			var user = _identity.GetCurrentUser();
			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var discussion = await _moduleDiscussionCommand.FirstOrDefaultAsync(x => x.ModuleId == moduleId && x.IsActive, ct);
			if (discussion == null)
			{
				response.SetMessage(MessageId.E00000, "Discussion not found for the specified module.");
				return response;
			}

			var parentComment = await _moduleDiscussionCommentCommand.FirstOrDefaultAsync(x => x.CommentId == parentCommentId && x.IsActive, ct);
			if (parentComment == null)
			{
				response.SetMessage(MessageId.E00000, "Parent comment not found.");
				return response;
			}

			var entity = new ModuleDiscussionComment
			{
				DiscussionId = discussion.DiscussionId,
				UserId = user.UserId,
				UserDisplayName = user.FullName,
				Content = content,
				ParentCommentId = parentCommentId
			};

			await _moduleDiscussionCommentCommand.AddAsync(entity, user.Email);
			await _uow.SaveChangesAsync(user.Email, ct);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Trả lời");

			return response;
		}
	}
}
