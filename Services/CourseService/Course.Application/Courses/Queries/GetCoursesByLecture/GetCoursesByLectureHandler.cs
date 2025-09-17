using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.GetCoursesByTeacherId
{
	public class GetCoursesByLectureHandler(ICourseService _courseService) : IQueryHandler<GetCoursesByLectureQuery, GetCoursesByTeacherIdResponse>
	{
		public async Task<GetCoursesByTeacherIdResponse> Handle(GetCoursesByLectureQuery request, CancellationToken cancellationToken)
		{
			var response = await _courseService.GetAllAsync(request.Pagination, request.Filter, cancellationToken);
			
			return new GetCoursesByTeacherIdResponse
			{
				Success = response.Success,
				Message = response.Message,
				Response = response.Response
			};
		}
	}
}
