using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.StudentService;
using Course.Domain.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Comments;

public class MappingSubjectCodeWithMajorCodeEventConsumer(ICommandRepository<SyllabusSubject> syllabusSubjectRepository) : IConsumer<MappingSubjectCodeWithMajorCodeEvent>
{
    public async Task Consume(ConsumeContext<MappingSubjectCodeWithMajorCodeEvent> context)
    {
        var evt = context.Message;
        var response = new MappingSubjectCodeWithMajorCodeEventResponse { Success = false };

        try
        {
            // Query để lấy tất cả MajorCode theo SubjectCode
            var mappings = await syllabusSubjectRepository
                .Find(ss => ss.Subject != null 
                            && ss.Syllabus != null 
                            && ss.Syllabus.Major != null
                            && evt.SubjectCodes.Contains(ss.Subject.SubjectCode) 
                            && ss.Syllabus.IsActive 
                            && ss.Subject.IsActive)
                .Include(x => x.Syllabus)
                .ThenInclude(x => x.Major)
                .Select(ss => new MappingSubjectCodeWithMajorCodeEventResponseEntity
                {
                    SubjectCode = ss.Subject.SubjectCode,
                    MajorCode = ss.Syllabus.Major.MajorCode
                })
                .Distinct()
                .ToListAsync(context.CancellationToken);

            response.Response = mappings;
            response.Success = true;
            response.Message = $"Đã tìm thấy {mappings.Count} mapping giữa SubjectCode và MajorCode";
            
            await context.RespondAsync(response);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Lỗi khi mapping SubjectCode với MajorCode: {ex.Message}";
            await context.RespondAsync(response);
        }
    }
}