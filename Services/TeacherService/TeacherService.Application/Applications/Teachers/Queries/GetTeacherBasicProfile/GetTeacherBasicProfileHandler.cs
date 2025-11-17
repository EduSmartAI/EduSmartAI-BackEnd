using BuildingBlocks.CQRS;
using TeacherService.Application.Interfaces;

namespace TeacherService.Application.Applications.Teachers.Queries.GetTeacherBasicProfile
{
	public class GetTeacherBasicProfileHandler(ITeacherService _teacherService) : IQueryHandler<GetTeacherBasicProfileQuery, GetTeacherBasicProfileResponse>
	{
		public async Task<GetTeacherBasicProfileResponse> Handle(GetTeacherBasicProfileQuery request, CancellationToken ct)
		{
			return await _teacherService.GetBasicProfileAsync(request.TeacherId, ct);
		}
	}
}
