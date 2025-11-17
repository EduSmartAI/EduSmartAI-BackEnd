using BaseService.API.BaseControllers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using TeacherService.Application.Applications.Teachers.Commands.UpdateTeacherProfile;
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
		public async Task<GetTeacherDetailResponse> GetById([FromRoute] Guid teacherId)
		{
			var query = new GetTeacherDetailQuery(teacherId);

			return await ApiControllerHelper.HandleRequest<
				GetTeacherDetailQuery,
				GetTeacherDetailResponse,
				TeacherDetailDto>(
				query,
				_logger,
				ModelState,
				async () => await _sender.Send(query),
				new GetTeacherDetailResponse());
		}

		[HttpPut("{teacherId:guid}")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
