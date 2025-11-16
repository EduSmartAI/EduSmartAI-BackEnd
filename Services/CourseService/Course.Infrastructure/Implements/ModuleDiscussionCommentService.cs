using BaseService.Application.Common;
using Course.Application.Comments.ModuleDiscussionComment.Commands.PostModuleDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Commands.ReplyDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread;
using Course.Application.DTOs.CommentsDTO;
using Course.Infrastructure.Caching;

namespace Course.Infrastructure.Implements
{
	public class ModuleDiscussionCommentService(
		IIdentityService _identity,
		IUnitOfWork _uow,
		ICommandRepository<ModuleDiscussionComment> _moduleDiscussionCommentCommand,
		ICommandRepository<ModuleDiscussion> _moduleDiscussionCommand,
		IDatabase _cache
	) : IModuleDiscussionCommentService
	{
		/// <summary>
		/// Get Discussion Thread
		/// </summary>
		/// <param name="moduleId"></param>
		/// <param name="page"></param>
		/// <param name="size"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetDiscussionThreadResponse> GetThreadAsync(Guid moduleId, int? page, int? size, CancellationToken ct = default)
		{
			var response = new GetDiscussionThreadResponse { Success = false };

			var pageNumber = page.GetValueOrDefault(1);
			var pageSize = size.GetValueOrDefault(20);

			// Cache key
			var cacheKey = BuildDiscussionThreadCacheKey(moduleId, pageNumber, pageSize);

			// Try cache
			var cached = await _cache.GetAsync<PagedResult<DiscussionCommentDto>>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.Response = cached;
				response.SetMessage(MessageId.I00001, "Lấy chuỗi thảo luận (cache)");
				return response;
			}

			// Find discussion
			var discussion = await _moduleDiscussionCommand
				.Find(
					predicate: d => d.ModuleId == moduleId && d.IsActive,
					isTracking: false,
					cancellationToken: ct)
				.OrderBy(d => d.CreatedAt)
				.FirstOrDefaultAsync(ct);

			if (discussion is null)
			{
				var emptyResult = new PagedResult<DiscussionCommentDto>
				{
					Items = new List<DiscussionCommentDto>(),
					TotalCount = 0,
					PageNumber = pageNumber,
					PageSize = pageSize
				};

				response.Success = true;
				response.Response = emptyResult;
				await _cache.SetAsync(cacheKey, emptyResult, TimeSpan.FromMinutes(5));

				return response;
			}

			var discussionId = discussion.DiscussionId;

			// 1) Load toàn bộ comments của discussion
			var allComments = await _moduleDiscussionCommentCommand
				.Find(
					predicate: x => x.DiscussionId == discussionId && x.IsActive,
					isTracking: false,
					cancellationToken: ct)
				.OrderBy(x => x.CreatedAt)   // đảm bảo thứ tự reply
				.ToListAsync(ct);

			// 2) Tách root & children
			var rootComments = allComments
				.Where(c => c.ParentCommentId == null)
				.ToList();

			// lookup chỉ chứa các comment có ParentCommentId != null
			var childrenLookup = allComments
				.Where(c => c.ParentCommentId != null)
				.GroupBy(c => c.ParentCommentId!.Value)   // Guid, không null
				.ToDictionary(
					g => g.Key,               // ParentCommentId (Guid)
					g => g.ToList()
				);

			// 3) Hàm đệ quy build cây nhiều tầng
			DiscussionCommentDto BuildTree(ModuleDiscussionComment c)
			{
				if (!childrenLookup.TryGetValue(c.CommentId, out var childrenEntities))
				{
					return new DiscussionCommentDto(
						c.CommentId,
						c.ParentCommentId,
						c.DiscussionId,
						c.UserId,
						c.UserDisplayName,
						c.Content,
						(DateTimeOffset)c.CreatedAt,
						Array.Empty<DiscussionCommentDto>()
					);
				}

				var children = childrenEntities
					.Select(child => BuildTree(child))
					.ToList();

				return new DiscussionCommentDto(
					c.CommentId,
					c.ParentCommentId,
					c.DiscussionId,
					c.UserId,
					c.UserDisplayName,
					c.Content,
					(DateTimeOffset)c.CreatedAt,
					children
				);
			}

			var totalRoot = rootComments.Count;

			// 4) Paging chỉ root-level
			var pagedRoots = rootComments
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			// Build cây cho từng root
			var items = pagedRoots.Select(c => BuildTree(c)).ToList();

			var result = new PagedResult<DiscussionCommentDto>
			{
				Items = items,
				TotalCount = totalRoot,
				PageNumber = pageNumber,
				PageSize = pageSize
			};

			// 5) Cache kết quả
			await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

			response.Success = true;
			response.Response = result;
			response.SetMessage(MessageId.I00001, "Lấy chuỗi thảo luận");
			return response;
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

			await ClearDiscussionThreadCacheAsync(moduleId);

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

			await ClearDiscussionThreadCacheAsync(moduleId);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Trả lời");

			return response;
		}

		#region Private Helpers
		private static string BuildDiscussionThreadCacheKey(Guid moduleId, int pageNumber, int pageSize)
			=> $"module:{moduleId}:discussion:thread:p{pageNumber}:s{pageSize}";

		private async Task ClearDiscussionThreadCacheAsync(Guid moduleId)
		{
			int[] commonPageSizes = { 10, 20, 50 };
			const int MaxPagesToClear = 5;

			var tasks = new List<Task>();

			foreach (var size in commonPageSizes)
			{
				for (int page = 1; page <= MaxPagesToClear; page++)
				{
					var key = BuildDiscussionThreadCacheKey(moduleId, page, size);
					tasks.Add(_cache.KeyDeleteAsync(key));
				}
			}

			await Task.WhenAll(tasks);
		}


		#endregion
	}
}
