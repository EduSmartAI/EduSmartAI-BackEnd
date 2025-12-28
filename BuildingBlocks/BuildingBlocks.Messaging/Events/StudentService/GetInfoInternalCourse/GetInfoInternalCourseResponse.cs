using System.Collections.Generic;
using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse
{
    public record GetInfoInternalCourseResponse : AbstractApiResponse<IReadOnlyList<InternalCourseInfoDto>>
    {
        public override IReadOnlyList<InternalCourseInfoDto> Response { get; set; } = [];
    }
    public record class InternalCourseInfoDto
    {
        public required string MajorCode { get; init; }
        public short SemesterNumber { get; init; }
        public required string SemesterCode { get; init; }
        public required string SemesterName { get; init; }
        public required string SubjectCode { get; init; }
        public required string SubjectName { get; init; }
        public Guid? CourseId { get; init; }
        public string? CourseTitle { get; init; }
        public short? Level { get; init; }
        public decimal? Price { get; init; }
        public decimal? DealPrice { get; init; }
        public int? DurationMinutes { get; init; }
        public string? Slug { get; init; }
        public string? CourseImageUrl { get; init; }
        public int? LearnerCount { get; init; }
        public bool? IsActive { get; init; }
        public decimal? DurationHours { get; init; }
        public string Description { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public bool isEnrolled { get; set; }
        public bool isWishList { get; set; }
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public List<string> TagNames { get; set; } = new();
    }
}
