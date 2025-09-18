using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CourseTagsDTO;

namespace Course.Application.Courses.Queries.GetCourseTags
{
	public record GetCourseTagsQuery : IQuery<GetCourseTagsResponse>;

	public record GetCourseTagsResponse : AbstractApiResponse<List<CourseTagDetailsDto>>
	{
		public override List<CourseTagDetailsDto> Response { get; set; }
	}
}
