using BaseService.Application.Common;
using Course.Application.DTOs.CommentsDTO;

namespace Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread
{
	public record GetDiscussionThreadQuery(Guid ModuleId, int? Page, int? Size) : IQuery<GetDiscussionThreadResponse>;

	public sealed record GetDiscussionThreadResponse : AbstractApiResponse<PagedResult<DiscussionCommentDto>> 
	{
		public override PagedResult<DiscussionCommentDto> Response { get; set; } = default!; 
	}
}
