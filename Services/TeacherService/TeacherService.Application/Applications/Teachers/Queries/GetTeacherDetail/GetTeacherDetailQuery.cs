using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using TeacherService.Application.DTOs;

namespace TeacherService.Application.Applications.Teachers.Queries.GetTeacherDetail
{
	public sealed record GetTeacherDetailQuery(Guid TeacherId) : IQuery<GetTeacherDetailResponse>;

	public sealed record GetTeacherDetailResponse : AbstractApiResponse<TeacherDetailDto>
	{
		public override TeacherDetailDto Response { get; set; } = default!;
	}

}
