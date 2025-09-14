using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.UpdateCourseModules
{
	internal class UpdateCourseModulesHandler(ICourseService courseService) : ICommandHandler<UpdateCourseModulesCommand, UpdateCourseModulesResponse>
	{
		public async Task<UpdateCourseModulesResponse> Handle(UpdateCourseModulesCommand request, CancellationToken cancellationToken)
		{
			return await courseService.UpdateCourseModulesAsync(request.CourseId, request.UpdateCourseModules, cancellationToken);
		}
	}
}
