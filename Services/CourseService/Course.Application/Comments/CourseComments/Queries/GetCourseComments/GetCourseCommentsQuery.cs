using BaseService.Application.Common;
using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.CourseComments.Queries.GetCourseComments
{
	public record GetCourseCommentsQuery(Guid CourseId, int? Page = 1, int? Size = 20) : IQuery<GetCourseCommentsResponse>;

	public sealed record GetCourseCommentsResponse : AbstractApiResponse<PagedResult<CourseCommentDto>>
	{
		public override PagedResult<CourseCommentDto> Response { get; set; } = new();
	}
}
