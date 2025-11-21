using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using TeacherService.Application.DTOs;

namespace TeacherService.Application.Applications.Teachers.Commands.UpdateTeacherProfile
{
	public sealed record UpdateTeacherProfileCommand(Guid TeacherId, UpdateTeacherProfileRequest Payload) : ICommand<UpdateTeacherProfileResponse>;

	public sealed record UpdateTeacherProfileResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = default!;
	}
}
