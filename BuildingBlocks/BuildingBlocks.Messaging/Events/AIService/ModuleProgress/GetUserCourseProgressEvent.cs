using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.ModuleProgress
{
    public sealed record GetUserCourseProgressEvent(Guid CourseId, Guid StudentId);

    public sealed record GetUserCourseProgressResponse : AbstractApiResponse<UserCourseProgressDto>
    {
        public override UserCourseProgressDto Response { get; set; } = new UserCourseProgressDto();
    }

    public sealed class UserCourseProgressDto
    {
        public long LessonsTotal { get; set; }
        public long LessonsCompleted { get; set; }
        public double PercentCompleted { get; set; }
    }
}
