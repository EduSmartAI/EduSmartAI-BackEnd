using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class CourseSelectsBySubjectCodeEventConsumer(ICommandRepository<CourseEntity> courseRepository) : IConsumer<CourseSelectsBySubjectCodeEvent>
{
    public async Task Consume(ConsumeContext<CourseSelectsBySubjectCodeEvent> context)
    {
        var response = new CourseSelectsBySubjectCodeEventResponse { Success = false };

        var coursesData = await courseRepository
            .Find(
                c => c.Subject.SyllabusSubjects.Any(ss => ss.Subject.SubjectCode == context.Message.SubjectCode) &&
                     c.IsActive,
                false,
                cancellationToken: CancellationToken.None,
                 c => c.Subject,
                c => c.Subject.SyllabusSubjects
            ).ToListAsync();
        if (!coursesData.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy môn học nào với mã môn học đã cho.");
            await context.RespondAsync(response);
            return;
        }
        
        // True
        response.Success = true;
        response.Response = coursesData
            .Select(c => new CourseSelectsBySubjectCodeEventResponseEntity
            {
                CourseId = c.CourseId
            })
            .ToList();
        await context.RespondAsync(response);
    }
}