using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.UpdateModule
{
	internal class UpdateModuleHandler(IModuleService moduleService) : ICommandHandler<UpdateModuleCommand, UpdateModuleResponse>
	{
		public async Task<UpdateModuleResponse> Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
		{
			return await moduleService.UpdateModuleAsync(request.ModuleId, request.UpdateModuleDto, cancellationToken);
		}
	}
}
