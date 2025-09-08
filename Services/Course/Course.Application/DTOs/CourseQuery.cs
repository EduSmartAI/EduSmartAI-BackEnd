namespace Course.Application.DTOs
{
	public record CourseQuery(
		string? Search = null,       // search theo title/description
		Guid? SubjectId = null,
		Guid? TeacherId = null,
		bool? IsActive = null
	);
}
