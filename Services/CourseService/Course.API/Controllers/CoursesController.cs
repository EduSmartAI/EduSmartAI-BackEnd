using BaseService.API.BaseControllers;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CoursesController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Get list of courses with pagination and optional filtering
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
				async () => await sender.Send(request),
				new GetCoursesResponse()
			);
		}

		/// <summary>
		/// Create a new course
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<IActionResult> ProcessRequestPost([FromBody] CreateCourseCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateCourseCommand, CreateCourseResponse, CourseDetailDto>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateCourseResponse()
			);
		}

		[HttpPut("{id:guid}")]
		public async Task<IActionResult> ProcessRequestPut([FromRoute] Guid id, [FromBody] UpdateCourseDto payload)
		{
			var request = new UpdateCourseCommand(id, payload);
			return await ApiControllerHelper.HandleRequest<UpdateCourseCommand, UpdateCourseResponse, CourseDetailDto>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new UpdateCourseResponse()
			);
		}
	}
}
