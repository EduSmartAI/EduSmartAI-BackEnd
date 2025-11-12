using Course.Application.LessonNotes.Commands.CreateNote;
using Course.Application.LessonNotes.Commands.DeleteNote;
using Course.Application.LessonNotes.Commands.UpdateNote;
using Course.Application.LessonNotes.Queries.GetLessonNotes;

namespace Course.Infrastructure.Implements
{
	public class LessonNoteService(
		IUnitOfWork unitOfWork,
		IIdentityService _identityService,
		ICommandRepository<Note> _noteCommandRepository,
		ICourseCache _cache
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

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Xóa note");
			return response;
		}

		public Task<GetLessonNotesResponse> GetByLessonAsync(Guid lessonId, int? page, int? size, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}

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

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Cập nhật note");

			return response;
		}
	}
}
