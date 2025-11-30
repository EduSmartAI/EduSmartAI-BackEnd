using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using Course.Domain.Models;

namespace Course.Application.Consumers;

public class MajorAndSemesterSelectEventConsumer(ICommandRepository<Semester> semesterRepository, ICommandRepository<Major> majorRepository) : IConsumer<MajorAndSemesterSelectEvent>
{
    public async Task Consume(ConsumeContext<MajorAndSemesterSelectEvent> context)
    {
        var response = new MajorAndSemesterSelectEventResponse
        {
            Success = false,
            Response =  new MajorAndSemesterSelectEventResponseEntity
            {
                Major = new MajorSelectEventResponseEntity(),
                Semester = new SemesterSelectEventResponseEntity()
            }
        };

        var evt = context.Message;
        if (evt.MajorId.HasValue)
        {
            var majorSelect = await majorRepository.FirstOrDefaultAsync(m => m.MajorId == evt.MajorId && m.IsActive);
            if (majorSelect == null)
            {
                response.SetMessage(MessageId.I00000, "Không tìm thấy ngành học.");
                await context.RespondAsync(response);
                return;
            }
            var majorSelectEntity = new MajorSelectEventResponseEntity
            {
                MajorId = majorSelect.MajorId,
                MajorName = majorSelect.MajorName,
                MajorCode = majorSelect.MajorCode
            };
            response.Response.Major = majorSelectEntity;
        } 
        if (evt.SemesterId.HasValue)
        {
            var semesterSelect = await semesterRepository.FirstOrDefaultAsync(s => s.SemesterId == evt.SemesterId);
            if (semesterSelect == null)
            {
                response.SetMessage(MessageId.I00001, "Không tìm thấy học kỳ.");
                await context.RespondAsync(response);
                return;
            }
            var semesterSelectEntity = new SemesterSelectEventResponseEntity
            {
                SemesterId = semesterSelect.SemesterId,
                SemesterName = semesterSelect.SemesterName,
                SemesterNumber = semesterSelect.SemesterNumber
            };
            response.Response.Semester = semesterSelectEntity;
        }
        await context.RespondAsync(response);
    }
}