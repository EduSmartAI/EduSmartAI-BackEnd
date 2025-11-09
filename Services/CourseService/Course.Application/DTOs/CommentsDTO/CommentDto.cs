namespace Course.Application.DTOs.CommentsDTO
{
	public record CommentDto(
		Guid CommentId,
		Guid CourseId,
		Guid UserId,
		string UserDisplayName,
		string Content,
		Guid? ParentCommentId,
		bool IsAnswer,
		bool IsActive,
		int ReplyCount,
		DateTimeOffset CreatedAt
	);
}
