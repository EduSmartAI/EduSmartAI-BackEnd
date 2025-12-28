using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.AddLearningPathCourse
{
	public class AddLearningPathCourseHandler(ILearningPathService _learningPathService) : ICommandHandler<AddLearningPathCourseCommand, AddLearningPathCourseResponse>
	{
		public async Task<AddLearningPathCourseResponse> Handle(AddLearningPathCourseCommand request, CancellationToken cancellationToken)
		{
			return await _learningPathService.AddLearningPathCourseAsync(request, cancellationToken);
		}
	}
}
