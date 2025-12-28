namespace Course.Application.DTOs.CommentsDTO
{
	public sealed record DiscussionCommentDto(
		Guid CommentId, 
		Guid? ParentCommentId, 
		Guid DiscussionId, 
		Guid UserId,
		string UserDisplayName, 
		string Content, 
		DateTimeOffset CreatedAt, 
		IReadOnlyList<DiscussionCommentDto> Replies);
}
