namespace Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress
{
	public class UpdateUserLessonProgressHandler(IStudentProgressService _studentProgressService) : ICommandHandler<UpdateUserLessonProgressCommand, UpdateUserLessonProgressResponse>
	{
		public Task<UpdateUserLessonProgressResponse> Handle(UpdateUserLessonProgressCommand request, CancellationToken cancellationToken)
		{
			return _studentProgressService.UpdateUserLessonProgressAsync(request.UpdateUserLessonProgress, cancellationToken);
		}
	}
}
