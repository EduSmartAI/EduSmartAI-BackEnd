using BaseService.Application.Common;
using Course.Application.Comments.CourseComments.Commands.CreateComment;
using Course.Application.Comments.CourseComments.Commands.ReplyToComment;
using Course.Application.Comments.CourseComments.Queries.GetCourseComments;
using Course.Application.DTOs.CommentsDTO;

namespace Course.Infrastructure.Implements
{
	public sealed class CommentService(
		IDatabase _cache,
		IIdentityService _identityService,
		IUnitOfWork unitOfWork,
		ICommandRepository<CourseComment> _commentCmd,
		ICommandRepository<CourseStudentEnrollment> _enrollCmd
	) : ICommentService
	{
		/// <summary>
		/// Create Comment
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="content"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateCommentResponse> CreateAsync(Guid courseId, string content, CancellationToken ct = default)
		{
			var response = new CreateCommentResponse { Success = false };

			var user = _identityService.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			// check user da enroll khoa hoc hay chua
			var enrolled = await _enrollCmd.FirstOrDefaultAsync(x => x.CourseId == courseId && x.UserId == user.UserId && x.IsActive, ct);

			if (enrolled == null)
			{
				response.SetMessage(MessageId.E00000, "Bạn chưa tham gia khóa học này");
				return response;
			}

			var entity = new CourseComment
			{
				CourseId = courseId,
				UserId = user.UserId,
				UserDisplayName = user.FullName,
				Content = content,
				ParentCommentId = null
			};

			await _commentCmd.AddAsync(entity, user.Email);
			await unitOfWork.SaveChangesAsync(user.Email, ct);

			response.Success = true;
			response.Response = new CourseCommentDto(
				entity.CommentId, courseId, user.UserId, user.Email, content, null, true, 0, DateTimeOffset.UtcNow);
			response.SetMessage(MessageId.I00001, "Đã tạo bình luận");

			return response;
		}

		/// <summary>
		/// Get Course Comment
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="page"></param>
		/// <param name="size"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseCommentsResponse> GetCourseCommentsAsync(Guid courseId, int? page, int? size, CancellationToken ct = default)
		{
			var response = new GetCourseCommentsResponse { Success = false };

			var paged = await _commentCmd.PagedAsync(
			page, size,
			predicate: x => x.CourseId == courseId && x.IsActive,
			orderBy: x => x.CreatedAt,
			orderByDescending: true,
			cancellationToken: ct);

			var currentIds = paged.Items.Select(c => c.CommentId).ToList();

			if (currentIds.Count == 0)
			{
				response.Success = true;
				response.Response = new PagedResult<CourseCommentDto>
				{
					Items = new List<CourseCommentDto>(),
					TotalCount = paged.TotalCount,
					PageNumber = paged.PageNumber,
					PageSize = paged.PageSize
				};
				return response;
			}

			var replyCountRows = await _commentCmd
										.Find(
											predicate: x => x.CourseId == courseId
														 && x.IsActive
														 && x.ParentCommentId != null
														 && currentIds.Contains(x.ParentCommentId.Value),
											isTracking: false,
											cancellationToken: ct
										)
										.GroupBy(x => x.ParentCommentId)
										.Select(g => new { ParentId = g.Key!.Value, Count = g.Count() })
										.ToListAsync(ct);

			var replyCountDict = replyCountRows.ToDictionary(x => x.ParentId, x => x.Count);

			var items = paged.Items.Select(x =>
			{
				var replyCount = replyCountDict.TryGetValue(x.CommentId, out var n) ? n : 0;

				return new CourseCommentDto(
					x.CommentId,
					x.CourseId,
					x.UserId,
					x.UserDisplayName,
					x.Content, 
					x.ParentCommentId,
					x.IsActive,
					replyCount,
					x.CreatedAt
				);
			}).ToList();

			response.Success = true;
			response.Response = new PagedResult<CourseCommentDto>
			{
				Items = items,
				TotalCount = paged.TotalCount,
				PageNumber = paged.PageNumber,
				PageSize = paged.PageSize
			};

			return response;
		}

		/// <summary>
		/// Reply to a comment
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="parentCommentId"></param>
		/// <param name="content"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<ReplyToCommentResponse> ReplyAsync(Guid courseId, Guid parentCommentId, string content, CancellationToken ct = default)
		{
			var response = new ReplyToCommentResponse { Success = false };

			var user = _identityService.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var parent = await _commentCmd.FirstOrDefaultAsync(
				x => x.CommentId == parentCommentId && x.CourseId == courseId && x.IsActive,
				ct
			);

			if (parent == null)
			{
				response.SetMessage(MessageId.E00000, "Bình luận không tồn tại");
				return response;
			}

			var reply = new CourseComment
			{
				CommentId = Guid.NewGuid(),
				CourseId = courseId,
				UserId = user.UserId,
				UserDisplayName = user.FullName,
				Content = content,
				ParentCommentId = parentCommentId
			};

			await _commentCmd.AddAsync(reply, user.Email);
			await unitOfWork.SaveChangesAsync(user.Email, ct);

			response.Success = true;
			response.Response = new CourseCommentDto(
				reply.CommentId,
				reply.CourseId,
				reply.UserId,
				user.Email,
				reply.Content,
				reply.ParentCommentId,
				reply.IsActive,
				0,
				reply.CreatedAt
			);
			response.SetMessage(MessageId.I00001, "Đã trả lời");

			return response;
		}
	}
}
