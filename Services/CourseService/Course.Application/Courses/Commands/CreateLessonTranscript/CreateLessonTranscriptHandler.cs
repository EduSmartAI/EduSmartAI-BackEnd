namespace Course.Application.Courses.Commands.CreateLessonTranscript
{
	public class CreateLessonTranscriptHandler(ILessonService _lessonService) : ICommandHandler<CreateLessonTranscriptCommand, CreateLessonTranscriptResponse>
	{
		public async Task<CreateLessonTranscriptResponse> Handle(CreateLessonTranscriptCommand request, CancellationToken cancellationToken)
		{
			return await _lessonService.CreateAsync(request, cancellationToken);
		}
	}
}
