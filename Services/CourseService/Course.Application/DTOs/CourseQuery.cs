using Course.Domain.Enum;

namespace Course.Application.DTOs
{
	public record CourseQuery(
		string? Search = null,         // search theo title/description/slug/shortDescription
		string? SubjectCode = null,    // search theo subject code (ILIKE)
		bool? IsActive = null,         // lọc course đang active
		CourseSortBy SortBy = CourseSortBy.Latest
	);
}
