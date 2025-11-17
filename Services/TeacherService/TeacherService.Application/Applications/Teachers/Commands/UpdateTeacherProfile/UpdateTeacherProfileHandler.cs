using BuildingBlocks.CQRS;
using TeacherService.Application.Interfaces;

namespace TeacherService.Application.Applications.Teachers.Commands.UpdateTeacherProfile
{
	public class UpdateTeacherProfileHandler(ITeacherService _teacherService) : ICommandHandler<UpdateTeacherProfileCommand, UpdateTeacherProfileResponse>
	{
		public async Task<UpdateTeacherProfileResponse> Handle(UpdateTeacherProfileCommand request, CancellationToken cancellationToken)
		{
			return await _teacherService.UpdateTeacherProfileAsync(request.TeacherId, request.Payload, cancellationToken);
		}
	}
}
