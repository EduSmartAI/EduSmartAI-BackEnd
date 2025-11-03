using Course.Application.Courses.Commands.CreateLessonTranscript;

namespace Course.Application.Interfaces
{
	public interface ILessonService
	{
		Task<CreateLessonTranscriptResponse> CreateAsync(CreateLessonTranscriptCommand dto, CancellationToken ct = default);
	}
}
