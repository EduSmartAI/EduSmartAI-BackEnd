using BaseService.Application.Common;
using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.Queries.GetCourseComments
{
	public record GetCourseCommentsQuery(Guid CourseId, int? Page = 1, int? Size = 20) : IQuery<GetCourseCommentsResponse>;

	public sealed record GetCourseCommentsResponse : AbstractApiResponse<PagedResult<CommentDto>>
	{
		public override PagedResult<CommentDto> Response { get; set; } = new();
	}
}
