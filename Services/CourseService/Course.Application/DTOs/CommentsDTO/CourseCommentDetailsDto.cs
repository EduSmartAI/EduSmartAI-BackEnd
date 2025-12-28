namespace Course.Application.DTOs.CommentsDTO
{
	public record CourseCommentDetailsDto(
		Guid CommentId,
		Guid CourseId,
		Guid UserId,
		string UserDisplayName,
		string Content,
		Guid? ParentCommentId,
		bool IsActive,
		int ReplyCount,
		DateTimeOffset CreatedAt
	);
}
