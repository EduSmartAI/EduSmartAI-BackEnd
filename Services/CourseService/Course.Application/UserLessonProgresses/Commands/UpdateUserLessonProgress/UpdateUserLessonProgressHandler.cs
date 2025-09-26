using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress
{
	public class UpdateUserLessonProgressHandler(ICourseService _courseService) : ICommandHandler<UpdateUserLessonProgressCommand, UpdateUserLessonProgressResponse>
	{
		public Task<UpdateUserLessonProgressResponse> Handle(UpdateUserLessonProgressCommand request, CancellationToken cancellationToken)
		{
			return _courseService.UpdateUserLessonProgressAsync(request.UpdateUserLessonProgress, cancellationToken);
		}
	}
}
