using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using TeacherService.Application.DTOs;

namespace TeacherService.Application.Applications.Teachers.Queries.GetTeacherBasicProfile
{
	public sealed record GetTeacherBasicProfileQuery(Guid TeacherId) : IQuery<GetTeacherBasicProfileResponse>;

	public sealed record GetTeacherBasicProfileResponse : AbstractApiResponse<TeacherBasicProfileDto>
	{
		public override TeacherBasicProfileDto Response { get; set; } = default!;
	}
}
