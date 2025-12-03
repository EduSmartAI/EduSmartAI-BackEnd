namespace Course.Application.DTOs.CoursesDTO.WishlistDTO
{
	public record WishlistItemDto(
		Guid WishlistId,
		Guid CourseId,
		string TeacherName,
		string CourseTitle,
		string CourseDescription,
		string CourseShortDescription,
		string CourseImageUrl,
		short? CourseLevel,
		decimal CoursePrice,
		decimal? CourseDealPrice,
		string CourseSlug,
		bool IsActive,
		DateTimeOffset CreatedAt
	);
}
