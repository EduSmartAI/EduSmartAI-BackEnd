using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress
{
	public class CreateUserLessonProgressHandler(IStudentProgressService _studentProgressService) : ICommandHandler<CreateUserLessonProgressCommand, CreateUserLessonProgressResponse>
	{
		public async Task<CreateUserLessonProgressResponse> Handle(CreateUserLessonProgressCommand request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.CreateUserLessonProgressAsync(request.UserLessonProgress, cancellationToken);
		}
	}
}
