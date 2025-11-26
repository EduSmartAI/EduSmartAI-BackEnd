using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateLearningPathStatus
{
	public class UpdateLearningPathStatusHandler(ILearningPathService _learningPathService) : ICommandHandler<UpdateLearningPathStatusCommand, UpdateLearningPathStatusResponse>
	{
		public async Task<UpdateLearningPathStatusResponse> Handle(UpdateLearningPathStatusCommand request, CancellationToken cancellationToken)
		{
			return await _learningPathService.UpdateLearningPathStatusAsync(request.LearningPathId, (LearningPathStatus)request.Status, cancellationToken);
		}
	}
}
