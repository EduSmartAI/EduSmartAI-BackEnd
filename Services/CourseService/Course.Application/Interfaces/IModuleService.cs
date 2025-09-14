using Course.Application.Courses.Commands.UpdateModule;
using Course.Application.DTOs.ModulesDTO;

namespace Course.Application.Interfaces
{
	public interface IModuleService
	{
		Task<UpdateModuleResponse> UpdateModuleAsync(Guid moduleId, UpdateModuleDto dto, CancellationToken ct = default);
	}
}
