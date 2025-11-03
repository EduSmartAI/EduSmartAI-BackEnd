using Course.Application.Courses.Commands.CreateLessonTranscript;

namespace Course.Infrastructure.Implements
{
	public class LessonService(
		ICommandRepository<LessonTranscript> _lessonTranscriptCommandRepository,
		IUnitOfWork unitOfWork) : ILessonService
	{
		public async Task<CreateLessonTranscriptResponse> CreateAsync(CreateLessonTranscriptCommand dto, CancellationToken ct = default)
		{
			var response = new CreateLessonTranscriptResponse { Success = false };

			var lessonTranscript = new LessonTranscript
			{
				LessonId = dto.LessonId,
				Language = dto.Language,
				Status = dto.Status,
				TextFull = dto.TextFull,
				VttUrl = dto.VttUrl,
				VttPublicId = dto.VttPublicId,
				Error = dto.Error,
				IsActive = true,
			};

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				await _lessonTranscriptCommandRepository.AddAsync(lessonTranscript);
				await unitOfWork.SaveChangesAsync(ct);

				return true; // yêu cầu của BeginTransactionAsync: trả true để commit
			}, ct);

			response.Success = true;
			response.Response = lessonTranscript.TranscriptId.ToString();
			return response;

		}
	}
}
