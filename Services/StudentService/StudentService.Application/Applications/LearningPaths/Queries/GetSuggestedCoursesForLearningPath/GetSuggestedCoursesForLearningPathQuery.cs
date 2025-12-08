using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.Application.Applications.LearningPaths.Queries.GetSuggestedCoursesForLearningPath
{
	public sealed record GetSuggestedCoursesForLearningPathQuery(Guid PathId, string SubjectCode, SuggestedCourseType Mode) : IQuery<GetSuggestedCoursesForLearningPathResponse>;

	public sealed record GetSuggestedCoursesForLearningPathResponse : AbstractApiResponse<List<CourseBasicInfoDto>>
	{
		public override List<CourseBasicInfoDto> Response { get; set; } = new();
	}
}
