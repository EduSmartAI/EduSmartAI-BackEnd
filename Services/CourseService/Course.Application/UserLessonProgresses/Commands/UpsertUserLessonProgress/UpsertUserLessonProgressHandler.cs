namespace Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress
{
	public class UpsertUserLessonProgressHandler(IStudentProgressService _studentProgressService) : ICommandHandler<UpsertUserLessonProgressCommand, UpsertUserLessonProgressResponse>
	{
		public async Task<UpsertUserLessonProgressResponse> Handle(UpsertUserLessonProgressCommand request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.UpsertUserLessonProgressAsync(request.LessonId, request.UserLessonProgress, cancellationToken);
		}
	}
}
