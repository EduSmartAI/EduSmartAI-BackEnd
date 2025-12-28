namespace Course.Application.Courses.Commands.DeleteCourse
{
	public class DeleteCourseHandler(ICourseService _courseService) : ICommandHandler<DeleteCourseCommand, DeleteCourseResponse>
	{
		public async Task<DeleteCourseResponse> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
		{
			return await _courseService.DeleteCourseAsync(request.CourseId, cancellationToken);
		}
	}
}
