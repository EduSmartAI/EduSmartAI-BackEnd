using Course.Application.Courses.Commands.UpdateModule;

namespace Course.Application.Interfaces
{
	public interface IModuleService
	{
		Task<UpdateModuleResponse> UpdateModuleAsync(Guid moduleId, UpdateModuleDto dto, CancellationToken ct = default);
	}
}
