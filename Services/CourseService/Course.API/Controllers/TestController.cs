using BaseService.API.BaseControllers;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TestController : ControllerBase
	{
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private readonly IMediator _mediator;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mediator"></param>
		public TestController(IMediator mediator)
		{
			_mediator = mediator;
		}

		/// <summary>
		/// Incoming Post
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> ProcessRequest([FromQuery] GetCoursesQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetCoursesQuery, GetCoursesResponse, PaginatedResult<CourseDto>>(
				request,
				_logger,
				ModelState,
				async () => await _mediator.Send(request),
				new GetCoursesResponse()
			);
		}
	}
}