using BaseService.API.BaseControllers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.EnrollCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Commands.UpdateCourseModules;
using Course.Application.Courses.Queries.CheckEnrollment;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourseBySlug;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.Courses.Queries.GetCoursesByLecture;
using Course.Application.Courses.Queries.GetCourseTags;
using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.CourseTagsDTO;
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
		/// Get list of courses with pagination and optional filtering for lecture
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpGet("lecture")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get list of courses for lecture",
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

		/// <summary>
		/// Get course details by ID for guest users
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Get course details by ID for lectures
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
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
		/// Get course details by slug for guest users
		/// </summary>
		/// <param name="slug"></param>
		/// <returns></returns>
		[HttpGet("slug/{slug}")]
		[SwaggerOperation(
			Summary = "Get course details by slug for guest users",
			Description = "Retrieve detailed information about a specific course by its slug, including modules and lessons, accessible to guest users."
		)]
		public async Task<GetCourseBySlugForGuestResponse> ProcessRequestBySlug(string slug)
		{
			var query = new GetCourseBySlugForGuestQuery(slug);
			return await ApiControllerHelper.HandleRequest<GetCourseBySlugForGuestQuery, GetCourseBySlugForGuestResponse, CourseDetailForGuestDto>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseBySlugForGuestResponse()
			);
		}

		/// <summary>
		/// Get course details by slug for lectures
		/// </summary>
		/// <param name="slug"></param>
		/// <returns></returns>
		[HttpGet("auth/slug/{slug}")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get course details by slug for lectures",
			Description = "Retrieve detailed information about a specific course by its slug, including modules and lessons, accessible to lectures."
		)]
		public async Task<GetCourseBySlugForLectureResponse> ProcessRequestBySlugAuth(string slug)
		{
			var query = new GetCourseBySlugForLectureQuery(slug);
			return await ApiControllerHelper.HandleRequest<GetCourseBySlugForLectureQuery, GetCourseBySlugForLectureResponse, CourseDetailForLectureDto>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseBySlugForLectureResponse()
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
			Description = "Create a new course with its modules, lessons, and tags. Course tags are optional and can be used to categorize courses."
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

		/// <summary>
		/// Check if current user is enrolled in a course
		/// </summary>
		/// <param name="courseId"></param>
		/// <returns></returns>
		[HttpGet("{courseId}/enrollment")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Check if current user is enrolled in a course",
			Description = "Check if the authenticated user is enrolled in the specified course"
		)]
		public async Task<CheckEnrollmentResponse> CheckEnrollment([FromRoute] Guid courseId)
		{
			var query = new CheckEnrollmentQuery(courseId);

			return await ApiControllerHelper.HandleRequest<CheckEnrollmentQuery, CheckEnrollmentResponse, bool>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new CheckEnrollmentResponse()
			);
		}

		[HttpPost("{courseId:guid}/enrollment")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Enroll the current user in a course",
			Description = "Enroll the authenticated user in the specified course"
		)]
		public async Task<EnrollInCourseResponse> EnrollInCourse([FromRoute] Guid courseId)
		{
			var request = new EnrollInCourseCommand(courseId);
			return await ApiControllerHelper.HandleRequest<EnrollInCourseCommand, EnrollInCourseResponse, string>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new EnrollInCourseResponse()
			);
		}

		/// <summary>
		/// Get all course tags
		/// </summary>
		/// <returns></returns>
		[HttpGet("tags")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get all course tags",
			Description = "Retrieve all available course tags"
		)]
		public async Task<GetCourseTagsResponse> GetCourseTags()
		{
			var query = new GetCourseTagsQuery();

			return await ApiControllerHelper.HandleRequest<GetCourseTagsQuery, GetCourseTagsResponse, List<CourseTagDetailsDto>>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseTagsResponse()
			);
		}
	}
}
