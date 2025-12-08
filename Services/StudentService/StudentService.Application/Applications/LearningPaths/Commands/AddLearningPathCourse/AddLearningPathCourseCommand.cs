using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.AddLearningPathCourse
{
	public record AddLearningPathCourseCommand(
		Guid LearningPathMajorId,
		Guid? InternalCourseId,
		Guid? LearningPathSubjectCodeId,
		string SubjectCode
	) : ICommand<AddLearningPathCourseResponse>;

	public record AddLearningPathCourseResponse : AbstractApiResponse<LearningPathCourseDto>
	{
		public override LearningPathCourseDto Response { get; set; } = default!;
	}

	public class LearningPathCourseDto
	{
		public Guid LearningPathCourseId { get; set; }
		public Guid LearningPathMajorId { get; set; }
		public Guid? InternalCourseId { get; set; }
		public string SubjectCode { get; set; } = default!;
		public int? Position { get; set; }
		public short Status { get; set; }
	}

}
