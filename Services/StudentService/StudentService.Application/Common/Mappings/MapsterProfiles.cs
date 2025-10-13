using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using Mapster;
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
                .Map(d => d.SubjectCode, _ => null as string)
                .Map(d => d.SemesterPosition, s => s.Position.HasValue ? s.Position.Value : 0);

            // Enrich CourseItemDto from (Course + Info)
            config.NewConfig<(LearningPathCourseCollection c, InternalCourseInfoDto? info), CourseItemDto>()
                .Map(d => d.CourseId, s => s.c.InternalCourseId.HasValue ? s.c.InternalCourseId.Value.ToString() : null)
                .Map(d => d.SemesterPosition, s => s.info != null ? (int)s.info.SemesterNumber : 0)
                .Map(d => d.Description, s => s.info != null ? s.info.Description : string.Empty)
                .Map(d => d.ShortDescription, s => s.info != null ? s.info.ShortDescription : string.Empty)
                .Map(d => d.Title, s => s.info != null ? s.info.SubjectName : null)
                .Map(d => d.Slug, s => s.info != null ? s.info.Slug : null)
                .Map(d => d.SubjectCode, s => s.info != null ? s.info.SubjectCode : null)
                .Map(d => d.CourseImageUrl, s => s.info != null ? s.info.CourseImageUrl : null)
                .Map(d => d.LearnerCount, s => s.info != null ? s.info.LearnerCount : 0)
                .Map(d => d.DurationMinutes, s => s.info != null ? s.info.DurationMinutes : 0)
                .Map(d => d.DurationHours, s => s.info != null && s.info.DurationMinutes.HasValue
                        ? (int)Math.Ceiling(s.info.DurationMinutes.Value / 60m)
                        : 0)
                .Map(d => d.Level, s => s.info != null ? s.info.Level : 0)
                .Map(d => d.Price, s => s.info != null ? s.info.Price : 0m)
                .Map(d => d.DealPrice, s => s.info != null ? s.info.DealPrice : 0m);

            //// Major Internal -> InternalLearningPathDto
            config.NewConfig<LearningPathMajorCollection, InternalLearningPathDto>()
                .Map(d => d.MajorId, s => s.LearningPathMajorId.ToString())
                .Map(d => d.MajorCode, s => s.MajorCode)
                .Map(d => d.Reason, s => s.Reason)
                .Map(d => d.MajorCourse, _ => new List<CourseItemDto>());

            // Major External -> ExternalLearningPathDto
            config.NewConfig<LearningPathMajorCollection, ExternalLearningPathDto>()
                .Map(d => d.MajorId, s => s.LearningPathMajorId.ToString())
                .Map(d => d.MajorCode, s => s.MajorCode)
                .Map(d => d.Reason, s => s.Reason)
                .Map(d => d.Steps,
                    s => (s.LearningPathCourses != null ? s.LearningPathCourses : new List<LearningPathCourseCollection>())
                        .Where(c => c.InternalCourseId == null)
                        .GroupBy(c => new { c.StepName, c.Position })
                        .OrderBy(g => g.Key.Position.HasValue ? g.Key.Position.Value : int.MaxValue)
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
                // Status
                .Map(d => d.Status, s => s.Status)
                // Basic
                .Map(d => d.BasicLearningPath, s => new BasicLearningPathDto
                {
                    SubjectName = s.PathName,
                    Semester = null,
                    Courses = new List<CourseItemDto>()
                })
                // Internal list
                .Map(d => d.InternalLearningPath,
                    s => (s.LearningPathMajors != null ? s.LearningPathMajors : new List<LearningPathMajorCollection>())
                        .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
                        .Select(m => m.Adapt<InternalLearningPathDto>(config))
                        .ToList())
                // External list
                .Map(d => d.ExternalLearningPath,
                    s => (s.LearningPathMajors != null ? s.LearningPathMajors : new List<LearningPathMajorCollection>())
                        .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.External)
                        .Select(m => m.Adapt<ExternalLearningPathDto>(config))
                        .ToList());
        }
    }
}