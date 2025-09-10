using BuildingBlocks.Pagination;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CoursesController(ISender sender) : ControllerBase
	{
		[HttpGet]
		public async Task<ActionResult<GetCoursesResponse>> GetCourses(
			[FromQuery] int pageIndex = 0,
			[FromQuery] int pageSize = 10,
			[FromQuery] string? search = null,
			[FromQuery] Guid? subjectId = null,
			[FromQuery] Guid? teacherId = null,
			[FromQuery] bool? isActive = null,
			CancellationToken ct = default)
		{
			var pagination = new PaginationRequest(pageIndex, pageSize);

			var filter = new CourseQuery(
				Search: search,
				SubjectId: subjectId,
				TeacherId: teacherId,
				IsActive: isActive
			);

			var query = new GetCoursesQuery(pagination, filter);

			var result = await sender.Send(query, ct);
			return Ok(result);
		}
	}
}
