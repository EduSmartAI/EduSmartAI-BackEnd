namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseCommentDto(
		Guid CommentId,
		Guid UserId,
		string Content,
		Guid? ParentCommentId,
		DateTime CreatedAt,
		bool IsActive
	);
}
