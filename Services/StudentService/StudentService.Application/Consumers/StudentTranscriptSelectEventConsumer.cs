using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Consumers;

public class StudentTranscriptSelectEventConsumer(ICommandRepository<StudentTranscript> repository) : IConsumer<StudentTranscriptSelectEvent>
{
    public async Task Consume(ConsumeContext<StudentTranscriptSelectEvent> context)
    {
        var response = new StudentTranscriptSelectEventResponse {Success = false};
        var evt = context.Message;
        
        var studentTransciptSelects = await repository
            .Find(x => x.StudentId == evt.StudentId && x.IsActive)
            .Select(x => new StudentTranscriptSelectEventResponseEntity
            {
                SubjectCode = x.SubjectCode,
                Credit = x.Credit,
                Grade = x.Grade,
                Prerequisite = x.Prerequisite,
                Semester = x.Semester,
                SemesterNumber = x.SemesterNumber,
                Status = x.Status,
                StudentTranscriptId = x.StudentTranscriptId,
                SubjectName = x.SubjectName
            })
            .ToListAsync();
        if (!studentTransciptSelects.Any())
        {
            response.SetMessage(MessageId.I00000, "Không tìm thấy bảng điểm sinh viên.");
            await context.RespondAsync(response);
            return;
        }
        
        // True
        response.Success = true;
        response.Response = studentTransciptSelects;
        await context.RespondAsync(response);
    }
}