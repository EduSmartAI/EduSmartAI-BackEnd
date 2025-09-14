using BaseService.Application.Interfaces.Repositories;
using Course.Domain.Models;

namespace Course.Application.Interfaces
{
	public interface IModuleRepository : ICommandRepository<Module>
	{
	}
}
