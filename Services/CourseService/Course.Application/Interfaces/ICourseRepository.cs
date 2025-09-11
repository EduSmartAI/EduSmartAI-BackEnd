using BaseService.Application.Interfaces.Repositories;

namespace Course.Application.Interfaces
{
	public interface ICourseRepository : ICommandRepository<CourseEntity>
	{
	}
}
