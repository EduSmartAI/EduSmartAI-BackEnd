namespace Course.Application.DTOs.CoursesDTO.WishlistDTO
{
	public record WishlistItemDto(
		Guid WishlistId,
		Guid CourseId,
		string CourseTitle,
		string CourseSlug,
		bool IsActive,
		DateTimeOffset CreatedAt
	);
}
