using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress
{
	public class CreateUserLessonProgressHandler(ICourseService _courseService) : ICommandHandler<CreateUserLessonProgressCommand, CreateUserLessonProgressResponse>
	{
		public async Task<CreateUserLessonProgressResponse> Handle(CreateUserLessonProgressCommand request, CancellationToken cancellationToken)
		{
			return await _courseService.CreateUserLessonProgressAsync(request.UserLessonProgress, cancellationToken);
		}
	}
}
