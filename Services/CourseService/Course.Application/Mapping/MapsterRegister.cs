using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using Course.Application.DTOs.CoursesDTO;
using Course.Domain.Models;
using Mapster;

namespace Course.Application.Mapping
{
    public class MapsterRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // CourseEntity -> CourseDto
            config.NewConfig<CourseEntity, CourseDto>()
                .Map(d => d.SubjectCode, s => s.Subject != null ? s.Subject.SubjectCode : string.Empty)
                // các field trùng tên sẽ tự map: Title, ShortDescription, Slug, CourseImageUrl, ...
                // DurationHours là cột GENERATED (decimal?), Mapster sẽ lấy từ entity
                .IgnoreNullValues(true);
            config.NewConfig<CourseEntity, CourseDto>()
                .Map(d => d.SubjectCode, s => s.Subject != null ? s.Subject.SubjectCode : string.Empty)
                .IgnoreNullValues(true);

            config.NewConfig<VMajorSemesterSubjectCourses, InternalCourseInfoDto>()
                .Map(d => d.CourseId, s => s.CourseId ?? Guid.Empty)
                .Map(d => d.CourseTitle, s => s.CourseTitle)
                .Map(d => d.SubjectCode, s => s.SubjectCode)
                .Map(d => d.SubjectName, s => s.SubjectName)
                .Map(d => d.Level, s => s.Level)
                .Map(d => d.Price, s => s.Price)
                .Map(d => d.DealPrice, s => s.DealPrice)
                .Map(d => d.DurationMinutes, s => s.DurationMinutes)
                .Map(d => d.DurationHours, s => s.DurationHours)
                .Map(d => d.Slug, s => s.Slug)
                .Map(d => d.CourseImageUrl, s => s.CourseImageUrl)
                .Map(d => d.LearnerCount, s => s.LearnerCount)
                .Map(d => d.IsActive, s => s.IsActive)
                .Map(d => d.MajorCode, s => s.MajorCode)
                .Map(d => d.SemesterNumber, s => s.SemesterNumber)
                .Map(d => d.SemesterCode, s => s.SemesterCode)
                .Map(d => d.SemesterName, s => s.SemesterName)
                .Map(d => d.Description, s => s.Description)
                .Map(d => d.ShortDescription, s => s.ShortDescription)
                .IgnoreNullValues(true);
        }
    }
}

