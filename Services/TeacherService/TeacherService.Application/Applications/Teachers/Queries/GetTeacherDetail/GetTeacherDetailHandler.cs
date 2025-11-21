using BuildingBlocks.CQRS;
using TeacherService.Application.Interfaces;

namespace TeacherService.Application.Applications.Teachers.Queries.GetTeacherDetail
{
	public sealed class GetTeacherDetailHandler(ITeacherService _teacherService) : IQueryHandler<GetTeacherDetailQuery, GetTeacherDetailResponse>
	{
		public async Task<GetTeacherDetailResponse> Handle(GetTeacherDetailQuery request, CancellationToken cancellationToken)
		{
			return await _teacherService.GetDetailAsync(request.TeacherId, cancellationToken);
		}
	}

}
