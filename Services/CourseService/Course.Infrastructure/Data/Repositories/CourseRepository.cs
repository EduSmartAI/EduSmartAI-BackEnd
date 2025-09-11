using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Repositories;
using Course.Application.Interfaces;

namespace Course.Infrastructure.Data.Repositories
{
	public class CourseRepository : CommandRepository<CourseEntity>, ICourseRepository
	{
		public CourseRepository(AppDbContext context) : base(context)
		{
		}
	}
}
