using BaseService.API.BaseControllers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Commands.UpdateCourseModules;
using Course.Application.Courses.Commands.UpdateModule;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.Courses.Queries.GetCoursesByTeacherId;
using Course.Application.DTOs.CoursesDTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;

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
		[SwaggerOperation(
			Summary = "Get list of courses with pagination and optional filtering",
			Description = "Retrieve a paginated list of courses with optional filtering by title, category, or instructor."
		)]
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

		/// <summary>
		/// Get list of courses by teacher ID with pagination and optional filtering
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpGet("lecture")]
		//[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get list of courses by teacher ID",
			Description = "Retrieve a paginated list of courses created by a specific teacher with optional filtering."
		)]
		public async Task<GetCoursesByTeacherIdResponse> GetCoursesByTeacherId(
			//[FromRoute] Guid teacherId,
			[FromQuery] GetCoursesByLectureQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetCoursesByLectureQuery, GetCoursesByTeacherIdResponse, PaginatedResult<CourseDto>>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetCoursesByTeacherIdResponse()
			);
		}

		[HttpGet("{id:guid}")]
		[SwaggerOperation(
			Summary = "Get course details by ID for guest users",
			Description = "Retrieve detailed information about a specific course by its ID, including modules and lessons, accessible to guest users."
		)]
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

		[HttpGet("auth/{id:guid}")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get course details by ID for lectures",
			Description = "Retrieve detailed information about a specific course by its ID, including modules and lessons, accessible to lectures."
		)]
		public async Task<GetCourseByIdForLectureResponse> ProcessRequestByIdAuth(Guid id)
		{
			var query = new GetCourseByIdForLectureQuery(id);
			return await ApiControllerHelper.HandleRequest<GetCourseByIdForLectureQuery, GetCourseByIdForLectureResponse, CourseDetailForLectureDto>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseByIdForLectureResponse()
			);
		}

		/// <summary>
		/// Create a new course
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPost]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Create a new course",
			Description = "Create a new course with its modules and lessons"
		)]
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
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Update an existing course",
			Description = "Update an existing course (only course details, not modules or lessons)"
		)]
		public async Task<UpdateCourseResponse> ProcessRequestPut([FromRoute] Guid id, [FromBody] UpdateCourseCommand request)
		{
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
		/// Update multiple modules in a course (bulk update)
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="request"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		[HttpPut("{courseId}/modules")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Update multiple modules in a course",
			Description = "Update multiple modules in a course with its Objectives and Lessons"
		)]
		public async Task<UpdateCourseModulesResponse> UpdateCourseModules(
			[FromRoute] Guid courseId,
			[FromBody] UpdateCourseModulesCommand request)
		{
			return await ApiControllerHelper.HandleRequest<UpdateCourseModulesCommand, UpdateCourseModulesResponse, string>(
				request with { CourseId = courseId },
				_logger,
				ModelState,
				async () => await sender.Send(request with { CourseId = courseId }),
				new UpdateCourseModulesResponse()
			);
		}
	}
}
