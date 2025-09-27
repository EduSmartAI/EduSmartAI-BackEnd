using BaseService.API.BaseControllers;
using BaseService.Common.Utils.Const;
using Course.Application.Courses.Commands.EnrollCourse;
using Course.Application.Courses.Queries.CheckEnrollment;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourseBySlug;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;
using Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress;
using Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StudentLessonProgressController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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

		/// <summary>
		/// Enroll the current user in a course
		/// </summary>
		/// <param name="courseId"></param>
		/// <returns></returns>
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
		/// Create user lesson progress for a specific lesson
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPost]
		[Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Create user lesson progress",
			Description = "Create user lesson progress for a specific lesson"
		)]
		public async Task<CreateUserLessonProgressResponse> CreateUserLessonProgress([FromBody] CreateUserLessonProgressCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateUserLessonProgressCommand, CreateUserLessonProgressResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateUserLessonProgressResponse()
			);
		}

		/// <summary>
		/// Update user lesson progress for a specific lesson
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPut]
		[Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Update user lesson progress",
			Description = "Update user lesson progress for a specific lesson"
		)]
		public async Task<UpdateUserLessonProgressResponse> UpdateUserLessonProgress([FromBody] UpdateUserLessonProgressCommand request)
		{
			return await ApiControllerHelper.HandleRequest<UpdateUserLessonProgressCommand, UpdateUserLessonProgressResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new UpdateUserLessonProgressResponse()
			);
		}

		/// <summary>
		/// Get course details by ID for students
		/// </summary>
		/// <param name="courseId"></param>
		/// <returns></returns>
		[HttpGet("{courseId:guid}")]
		[Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get course details by ID for students",
			Description = "Retrieve detailed information about a specific course by its ID, including modules and lessons, accessible to students."
		)]
		public async Task<GetCourseByIdForStudentResponse> GetCourseByIdForStudentAsync(Guid courseId)
		{
			var query = new GetCourseByIdForStudentQuery(courseId);
			return await ApiControllerHelper.HandleRequest<GetCourseByIdForStudentQuery, GetCourseByIdForStudentResponse, CourseDetailForStudentDto>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseByIdForStudentResponse()
			);
		}

		/// <summary>
		/// Get course details by slug for students
		/// </summary>
		/// <param name="courseSlug"></param>
		/// <returns></returns>
		[HttpGet("{courseSlug}")]
		[Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Get course details by slug for students",
			Description = "Retrieve detailed information about a specific course by its slug, including modules and lessons, accessible to students."
		)]
		public async Task<GetCourseBySlugForStudentResponse> GetCourseBySlugForStudentAsync(string courseSlug)
		{
			var query = new GetCourseBySlugForStudentQuery(courseSlug);
			return await ApiControllerHelper.HandleRequest<GetCourseBySlugForStudentQuery, GetCourseBySlugForStudentResponse, CourseDetailForStudentDto>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new GetCourseBySlugForStudentResponse()
			);
		}
	}
}
