using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Repositories;
using Course.Application.Interfaces;
using Course.Domain.Models;

namespace Course.Infrastructure.Data.Repositories
{
	public class ModuleRepository : CommandRepository<Module>, IModuleRepository
	{
		public ModuleRepository(AppDbContext context) : base(context)
		{
		}
	}
}
