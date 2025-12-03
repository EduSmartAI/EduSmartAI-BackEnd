using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService;
using Course.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class SubjectCodeSelectEventConsumer(ICommandRepository<Subject> subjectRepository) : IConsumer<SubjectCodeSelectEvent>
{
    public async Task Consume(ConsumeContext<SubjectCodeSelectEvent> context)
    {
        var response = new SubjectCodeSelectEventResponse { Success = false };

        var subjectCodes = await subjectRepository
            .Find(x => x.IsActive)
            .Select(x => new SubjectCodeSelectEventResponseEntity
            {
                SubjectCode = x.SubjectCode
            }).ToListAsync();
        if (!subjectCodes.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy môn học nào.");
            await context.RespondAsync(response);
            return;
        }
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001);
        response.Response = subjectCodes;
        await context.RespondAsync(response);
    }
}