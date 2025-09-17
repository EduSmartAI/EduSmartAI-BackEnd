using Course.Domain.Enum;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseQuery(
		string? Search = null,         // search theo title/description/slug/shortDescription
		string? SubjectCode = null,    // search theo subject code (ILIKE)
		bool? IsActive = null,         // lọc course đang active
		Guid? LectureId = null,        // lọc course theo giảng viên
		CourseSortBy SortBy = CourseSortBy.Latest
	);
}
