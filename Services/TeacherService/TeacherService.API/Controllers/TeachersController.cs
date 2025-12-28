using BaseService.API.BaseControllers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;
using TeacherService.Application.Applications.Teachers.Commands.UpdateTeacherProfile;
using TeacherService.Application.Applications.Teachers.Queries.GetTeacherBasicProfile;
using TeacherService.Application.Applications.Teachers.Queries.GetTeacherDetail;
using TeacherService.Application.DTOs;

namespace TeacherService.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TeachersController(ISender _sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpGet("{teacherId:guid}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get teacher detail by Id")]
		public async Task<GetTeacherDetailResponse> GetById([FromRoute] Guid teacherId)
		{
			var query = new GetTeacherDetailQuery(teacherId);

			return await ApiControllerHelper.HandleRequest<GetTeacherDetailQuery, GetTeacherDetailResponse, TeacherDetailDto>(
				query,
				_logger,
				ModelState,
				async () => await _sender.Send(query),
				new GetTeacherDetailResponse());
		}

		[HttpGet("{teacherId:guid}/test/basicProfile")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get basic profile of a teacher by Id (for testing purposes only)")]
		public async Task<GetTeacherBasicProfileResponse> GetBasicProfileById([FromRoute] Guid teacherId)
		{
			var query = new GetTeacherBasicProfileQuery(teacherId);

			return await ApiControllerHelper.HandleRequest<GetTeacherBasicProfileQuery, GetTeacherBasicProfileResponse, TeacherBasicProfileDto>(
				query,
				_logger,
				ModelState,
				async () => await _sender.Send(query),
				new GetTeacherBasicProfileResponse());
		}

		[HttpPut("{teacherId:guid}")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Update teacher profile")]
		public async Task<UpdateTeacherProfileResponse> UpdateProfile([FromRoute] Guid teacherId, [FromBody] UpdateTeacherProfileRequest request)
		{
			var command = new UpdateTeacherProfileCommand(teacherId, request);

			return await ApiControllerHelper.HandleRequest<UpdateTeacherProfileCommand, UpdateTeacherProfileResponse, bool>(
				command,
				_logger,
				ModelState,
				async () => await _sender.Send(command),
				new UpdateTeacherProfileResponse());
		}

	}
}
