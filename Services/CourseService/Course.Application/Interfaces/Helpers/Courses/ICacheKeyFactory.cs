using BuildingBlocks.Pagination;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Interfaces.Helpers.Courses
{
	public interface ICacheKeyFactory
	{
		string GenerateCacheKeyForGetAll(PaginationRequest pagination, CourseQuery? query);
	}
}
