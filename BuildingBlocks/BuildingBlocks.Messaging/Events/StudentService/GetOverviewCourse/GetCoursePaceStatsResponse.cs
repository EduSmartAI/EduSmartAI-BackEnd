using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse
{
    public record GetCoursePaceStatsResponse : AbstractApiResponse<CoursePaceStatsDto>
    {
        public override CoursePaceStatsDto Response { get; set; } = new CoursePaceStatsDto();
    }
    public sealed class CoursePaceStatsDto
    {
        public double AverageMinutesPerLesson { get; set; }
        public int LearnerCount { get; set; }
        public int Rank { get; set; }          // 1 = nhanh nhất (ít phút/bài nhất)
        public int FasterCount { get; set; }   // số học viên cậu học nhanh hơn
        public int SlowerCount { get; set; }   // số học viên học nhanh hơn cậu
        public double FasterPercent { get; set; } // % học viên cậu nhanh hơn
    }
}
