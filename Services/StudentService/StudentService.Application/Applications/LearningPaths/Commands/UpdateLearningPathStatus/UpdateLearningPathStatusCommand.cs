using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateLearningPathStatus
{
	public record UpdateLearningPathStatusCommand(Guid LearningPathId, short Status) : ICommand<UpdateLearningPathStatusResponse>;

	public record UpdateLearningPathStatusResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get ; set ; }
	}
}
