using BaseService.Application.Common;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.LessonNotes.Commands.CreateNote;
using Course.Application.LessonNotes.Commands.DeleteNote;
using Course.Application.LessonNotes.Commands.UpdateNote;
using Course.Application.LessonNotes.Queries.GetLessonNotes;
using Course.Infrastructure.Caching;

namespace Course.Infrastructure.Implements
{
	public class LessonNoteService(
		IUnitOfWork unitOfWork,
		IIdentityService _identityService,
		ICommandRepository<Note> _noteCommandRepository,
		IDatabase _cache
	) : ILessonNoteService
	{
		/// <summary>
		/// Create Note for Lesson
		/// </summary>
		/// <param name="lessonId"></param>
		/// <param name="timeSeconds"></param>
		/// <param name="content"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateNoteResponse> CreateAsync(Guid lessonId, int timeSeconds, string content, CancellationToken ct = default)
		{
			var response = new CreateNoteResponse { Success = false };

			var user = _identityService.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var note = new Note
			{
				LessonId = lessonId,
				UserId = user.UserId,
				TimeSeconds = timeSeconds,
				Content = content
			};

			await _noteCommandRepository.AddAsync(note, user.Email);
			await unitOfWork.SaveChangesAsync(user.Email, ct);

			await ClearNotesCacheAsync(user.UserId, lessonId);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Tạo note");
			return response;
		}

		/// <summary>
		/// Delete Note
		/// </summary>
		/// <param name="noteId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<DeleteNoteResponse> DeleteAsync(Guid noteId, CancellationToken ct = default)
		{
			var response = new DeleteNoteResponse { Success = false };

			var user = _identityService.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var note = await _noteCommandRepository.FirstOrDefaultAsync(x => x.NoteId == noteId && x.UserId == user.UserId && x.IsActive, ct);

			if (note == null)
			{
				response.SetMessage(MessageId.E00000, "Note not found.");
				return response;
			}

			_noteCommandRepository.Update(note, user.Email, needLogicalDelete: true);
			await unitOfWork.SaveChangesAsync(user.Email, ct, needLogicalDelete: true);

			await ClearNotesCacheAsync(user.UserId, note.LessonId);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Xóa note");
			return response;
		}

		/// <summary>
		/// Get Notes by Lesson
		/// </summary>
		/// <param name="lessonId"></param>
		/// <param name="page"></param>
		/// <param name="size"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetLessonNotesResponse> GetByLessonAsync(Guid lessonId, int? page, int? size, CancellationToken ct = default)
		{
			var response = new GetLessonNotesResponse { Success = false };

			// 1) Lấy user hiện tại
			var user = _identityService.GetCurrentUser();
			if (user is null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var userId = user.UserId;
			var pageNumber = page.GetValueOrDefault(1);
			var pageSize = size.GetValueOrDefault(20);

			// 2) Tạo cache key
			var cacheKey = BuildNotesCacheKey(userId, lessonId, pageNumber, pageSize);

			// 3) Thử lấy từ cache
			var cached = await _cache.GetAsync<PagedResult<LessonNoteDto>>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.Response = cached;
				return response;
			}

			// 4) Query DB (chỉ note của user này, lesson này, còn active)
			var paged = await _noteCommandRepository.PagedAsync(
				pageNumber, pageSize,
				predicate: x => x.LessonId == lessonId
								&& x.UserId == userId
								&& x.IsActive,
				orderBy: x => x.TimeSeconds,   // hoặc CreatedAt, tuỳ bạn muốn sort kiểu gì
				orderByDescending: false,
				cancellationToken: ct);

			// 5) Map sang DTO
			var items = paged.Items.Select(x => new LessonNoteDto(
				x.NoteId,
				x.LessonId,
				x.TimeSeconds,
				x.Content,
				x.CreatedAt,
				x.UpdatedAt
			)).ToList();

			var result = new PagedResult<LessonNoteDto>
			{
				Items = items,
				TotalCount = paged.TotalCount,
				PageNumber = paged.PageNumber,
				PageSize = paged.PageSize
			};

			// 6) Lưu vào cache (TTL 5 phút, có thể chỉnh)
			await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

			// 7) Trả về response
			response.Success = true;
			response.Response = result;
			response.SetMessage(MessageId.I00001, "Lấy notes");

			return response;
		}

		/// <summary>
		/// Update Note
		/// </summary>
		/// <param name="noteId"></param>
		/// <param name="content"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpdateNoteResponse> UpdateAsync(Guid noteId, string content, CancellationToken ct = default)
		{
			var response = new UpdateNoteResponse { Success = false };

			var user = _identityService.GetCurrentUser();

			if (user == null)
			{
				response.SetMessage(MessageId.E00000, "User not authenticated.");
				return response;
			}

			var note = await _noteCommandRepository.FirstOrDefaultAsync(x => x.NoteId == noteId && x.UserId == user.UserId && x.IsActive, ct);

			if (note == null)
			{
				response.SetMessage(MessageId.E00000, "Note not found.");
				return response;
			}

			note.Content = content;
			_noteCommandRepository.Update(note, user.Email);
			await unitOfWork.SaveChangesAsync(user.Email, ct);

			await ClearNotesCacheAsync(user.UserId, note.LessonId);

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Cập nhật note");

			return response;
		}

		#region Private Helpers

		/// <summary>
		/// Build Cache Key for Lesson Notes
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="lessonId"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pageSize"></param>
		/// <returns></returns>
		private static string BuildNotesCacheKey(Guid userId, Guid lessonId, int pageNumber, int pageSize)
		=> $"user:{userId}:lesson:{lessonId}:notes:p{pageNumber}:s{pageSize}";

		/// <summary>
		/// Clear Notes Cache for a Lesson and User
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="lessonId"></param>
		/// <returns></returns>
		private async Task ClearNotesCacheAsync(Guid userId, Guid lessonId)
		{
			// Các pageSize phổ biến – tùy bạn chỉnh (nên giống GetByLessonAsync)
			int[] commonPageSizes = { 10, 20, 50 };

			// Số trang đầu cần clear (vì note hầu hết nằm trong page đầu)
			const int MaxPagesToClear = 5;

			var tasks = new List<Task>();

			foreach (var size in commonPageSizes)
			{
				for (int page = 1; page <= MaxPagesToClear; page++)
				{
					var key = $"user:{userId}:lesson:{lessonId}:notes:p{page}:s{size}";
					tasks.Add(_cache.KeyDeleteAsync(key));
				}
			}

			await Task.WhenAll(tasks);
		}

		#endregion
	}
}
