using BaseService.API.BaseControllers;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Commands.UpdateModule;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs.CoursesDTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Course.API.Controllers
{
	[Route("api/v1/[controller]")]
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
		public async Task<GetCoursesResponse> ProcessRequest([FromQuery] GetCoursesQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetCoursesQuery, GetCoursesResponse, PaginatedResult<CourseDto>>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetCoursesResponse()
			);
		}

		[HttpGet("{id:guid}")]
		public async Task<GetCourseByIdForGuestResponse> ProcessRequestById(Guid id)
		{
			var query = new GetCourseByIdForGuestQuery(id);

			return await ApiControllerHelper.HandleRequest<GetCourseByIdForGuestQuery, GetCourseByIdForGuestResponse, CourseDetailForGuestDto>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseByIdForGuestResponse()
			);
		}

		/// <summary>
		/// Create a new course
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<CreateCourseResponse> ProcessRequestPost([FromBody] CreateCourseCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateCourseCommand, CreateCourseResponse, string>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateCourseResponse()
			);
		}

		/// <summary>
		/// Update an existing course (only course details, not modules or lessons)
		/// </summary>
		/// <param name="id"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPut("{id:guid}")]
		public async Task<UpdateCourseResponse> ProcessRequestPut([FromRoute] Guid id, [FromBody] UpdateCourseCommand request)
		{
			//var request = new UpdateCourseCommand(id, payload);
			var response = new UpdateCourseResponse();

			if (request.CourseId == Guid.Empty)
				request = request with { CourseId = id };
			else if (request.CourseId != id)
			{
				response.Success = false;
				response.SetMessage("CourseId in route and payload do not match.");
				return response;
			}

			return await ApiControllerHelper.HandleRequest<UpdateCourseCommand, UpdateCourseResponse, string>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new UpdateCourseResponse()
			);
		}

		/// <summary>
		/// Update a module within a course, including its objectives and lessons
		/// </summary>
		/// <param name="id"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPut("Module/{id:guid}")]
		public async Task<UpdateModuleResponse> ProcessRequestPutModule([FromRoute] Guid id, [FromBody] UpdateModuleCommand request)
		{
			var response = new UpdateModuleResponse();
			if (request.ModuleId == Guid.Empty)
				request = request with { ModuleId = id };
			else if (request.ModuleId != id)
			{
				response.Success = false;
				response.SetMessage("ModuleId in route and payload do not match.");
				return response;
			}
			return await ApiControllerHelper.HandleRequest<UpdateModuleCommand, UpdateModuleResponse, string>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new UpdateModuleResponse()
			);
		}
	}
}
