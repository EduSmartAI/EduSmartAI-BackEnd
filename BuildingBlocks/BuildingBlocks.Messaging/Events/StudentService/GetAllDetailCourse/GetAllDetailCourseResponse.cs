using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse
{
    public sealed record GetAllDetailCourseResponse : AbstractApiResponse<CourseHierInfoDto>
    {
        public override CourseHierInfoDto Response { get; set; } = new();
    }

    public sealed class LessonInfor
    {
        public Guid LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
    }

    public sealed class CourseModuleDto
    {
        public Guid ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public bool IsCore { get; set; }
        public List<LessonInfor> Lessons { get; set; } = new();
    }

    public sealed class CourseHierInfoDto
    {
        public Guid CourseId { get; set; }
        public List<CourseModuleDto> Modules { get; set; } = new();
    }
}
