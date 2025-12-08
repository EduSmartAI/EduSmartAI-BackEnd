using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Application.DTOs.CoursesDTO
{
	public record GetSuggestedCoursesStudentServiceDto(
		Guid UserId,
		string SubjectCode,
		int CurrentLevel,
		SuggestedCourseType Type,
		List<Guid> ExistingCourseIds
	);
}
