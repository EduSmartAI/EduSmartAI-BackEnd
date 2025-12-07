using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using Mapster;
using System.Text.RegularExpressions;
using StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Common.Mappings
{
    public static class MapsterProfiles
    {
        public static void Register(TypeAdapterConfig config)
        {
            // Course -> CourseItemDto
            config.NewConfig<LearningPathCourseCollection, CourseItemDto>()
            .Map(d => d.CourseId, s => s.InternalCourseId.HasValue ? s.InternalCourseId.Value.ToString() : null)
            .Map(d => d.SubjectCode, s => s.SubjectCode ?? string.Empty)
            .Map(d => d.SemesterPosition, s => s.Position ?? 0)
            .Map(d => d.Status, s => s.Status);

            // Enrich CourseItemDto from (Course + Info)
            config.NewConfig<(LearningPathCourseCollection c, InternalCourseInfoDto? info), CourseItemDto>()
                .ConstructUsing(_ => new CourseItemDto())
                .Map(d => d.CourseId, s => s.c.InternalCourseId.HasValue ? s.c.InternalCourseId.Value.ToString() : null)
                // info.SemesterNumber -> c.Position -> 0
                .Map(d => d.SemesterPosition, s => s.info == null ? (s.c.Position ?? 0) : (int)s.info.SemesterNumber)
                .Map(d => d.Description, s => s.info == null ? string.Empty : s.info.Description)
                .Map(d => d.ShortDescription, s => s.info == null ? string.Empty : s.info.ShortDescription)
                .Map(d => d.Title, s => s.info == null ? null : s.info.SubjectName)
                .Map(d => d.Slug, s => s.info == null ? null : s.info.Slug)
                .Map(d => d.SubjectCode,
                    s => s.info != null
                        ? (s.info.SubjectCode ?? string.Empty)
                        : (s.c.SubjectCode ?? string.Empty))
                .Map(d => d.CourseImageUrl, s => s.info == null ? null : s.info.CourseImageUrl)
                .Map(d => d.LearnerCount, s => s.info != null && s.info.LearnerCount.HasValue ? s.info.LearnerCount.Value : 0)
                .Map(d => d.DurationMinutes, s => s.info != null && s.info.DurationMinutes.HasValue ? s.info.DurationMinutes.Value : 0)
                .Map(d => d.DurationHours, s => s.info != null && s.info.DurationMinutes.HasValue ? (int)Math.Ceiling(s.info.DurationMinutes.Value / 60m) : 0)
                .Map(d => d.Level, s => s.info != null && s.info.Level.HasValue ? (int)s.info.Level.Value : 0)
                .Map(d => d.Price, s => s.info != null && s.info.Price.HasValue ? s.info.Price.Value : 0m)
                .Map(d => d.DealPrice, s => s.info != null && s.info.DealPrice.HasValue ? s.info.DealPrice.Value : 0m)
                .Map(d => d.Status, s => s.c.Status)
                .Map(d => d.isEnrolled, s => s.info != null && s.info.isEnrolled)
                .Map(d => d.isWishList, s => s.info != null && s.info.isWishList)
                .Map(d => d.TeacherId, s => s.info == null ? null : s.info.TeacherId)
                .Map(d => d.TeacherName, s => s.info == null ? null : s.info.TeacherName)
                .Map(d => d.TagNames, s => s.info != null && s.info.TagNames != null
                    ? s.info.TagNames
                    : new List<string>());

            // Major Internal -> InternalLearningPathDto
            config.NewConfig<LearningPathMajorCollection, InternalLearningPathDto>()
                .Map(d => d.MajorId, s => s.LearningPathMajorId.ToString())
                .Map(d => d.MajorCode, s => s.MajorCode)
                .Map(d => d.Reason, s => s.Reason)
                .Map(d => d.PositionIndex, s => s.PositionIndex)
                .Map(d => d.MajorCourseGroups, _ => new List<CourseGroupDto>());

            // Major External -> ExternalLearningPathDto (giữ nguyên)
            config.NewConfig<LearningPathMajorCollection, ExternalLearningPathDto>()
                .Map(d => d.MajorId, s => s.LearningPathMajorId.ToString())
                .Map(d => d.MajorCode, s => s.MajorCode)
                .Map(d => d.Reason, s => s.Reason)
                .Map(d => d.Steps,
                    s => (s.LearningPathCourses ?? new List<LearningPathCourseCollection>())
                        .Where(c => c.InternalCourseId == null)
                        .GroupBy(c => new { c.StepName, c.Position })
                        .OrderBy(g => g.Key.Position ?? int.MaxValue)
                        .Select(g => new ExternalStepDto
                        {
                            Title = string.IsNullOrWhiteSpace(g.Key.StepName)
                                    ? $"Step {(g.Key.Position ?? 0)}"
                                    : g.Key.StepName,
                            Suggested_Courses = g.Select(c => new SuggestedCourseDto
                            {
                                Title = c.StepName,
                                Link = c.ExternalCourseLink,
                                Reason = c.ExternalCourseReason,
                                Level = c.ExternalCourseLevel,
                                Provider = c.ExternalCourseProvider
                            }).ToList()
                        })
                        .ToList()
                );

            // LearningPath -> LearningPathSelectDto
            config.NewConfig<LearningPathCollection, LearningPathSelectDto>()
                .Map(d => d.Status, s => s.Status)
                .Map(d => d.PathName, s => s.PathName)
                .Map(d => d.Level, s => s.Level)
                .Map(d => d.LevelReason, s => s.LevelReason)
                .Map(d => d.IsSkipTest, s => s.IsSkipTest)
                .Map(d => d.LimitTime, s => s.LimitTime)
                .Map(d => d.EvaluationAndImprove, s => s.EvaluationAndImprove)
                .Map(d => d.SummaryFeedback, s => s.SummaryFeedback)
                .Map(d => d.HabitAndInterestAnalysis, s => s.HabitAndInterestAnalysis)
                .Map(d => d.Personality, s => s.Personality)
                .Map(d => d.LearningAbility, s => s.LearningAbility)
                .Map(d => d.praticalAbilityFeedbacks, s => MapPracticalAbilityFeedbacks(s.AbilityFeedback))
                .Map(d => d.BasicLearningPath, s => new BasicLearningPathDto { CourseGroups = new List<CourseGroupDto>() })
                .Map(d => d.InternalLearningPath,
                    s => (s.LearningPathMajors ?? new List<LearningPathMajorCollection>())
                        .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
                        .Select(m => m.Adapt<InternalLearningPathDto>(config))
                        .ToList())
                .Map(d => d.ExternalLearningPath,
                    s => (s.LearningPathMajors ?? new List<LearningPathMajorCollection>())
                        .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.External)
                        .Select(m => m.Adapt<ExternalLearningPathDto>(config))
                        .ToList());

            // List -> SelectAll (giữ nguyên)
            config.NewConfig<LearningPathCollection, LearningPathSelectAllDto>()
                  .Map(d => d.PathId, s => s.PathId)
                  .Map(d => d.PathName, s => s.PathName)
                  .Map(d => d.CreatedAt, s => s.CreatedAt)
                  .Map(d => d.Status, s => s.Status);
        }

        private static List<PraticalAbilityFeedback> MapPracticalAbilityFeedbacks(string? abilityFeedback)
        {
            if (string.IsNullOrWhiteSpace(abilityFeedback))
            {
                return new List<PraticalAbilityFeedback>();
            }

            return Regex
                .Split(abilityFeedback.Replace("\r\n", "\n"), @"\r?\n?\*{5,}\r?\n?")
                .Select(segment => segment.Trim())
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment => new PraticalAbilityFeedback
                {
                    AnalysisMarkDown = segment
                })
                .ToList();
        }
    }
}