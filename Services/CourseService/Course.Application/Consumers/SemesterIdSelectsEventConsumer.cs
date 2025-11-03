using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.StudentService;
using Course.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Course.Application.Consumers;

public class SemesterIdSelectsEventConsumer(ICommandRepository<Semester> semesterRepository) : IConsumer<SemesterIdSelectsEvent>
{
    public async Task Consume(ConsumeContext<SemesterIdSelectsEvent> context)
    {
        var evt = context.Message;
        var semesterNumbers = evt.SemesterNumbers.Distinct().ToList();
        
        var semesters = await semesterRepository
            .Find(s => semesterNumbers.Contains(s.SemesterNumber))
            .ToListAsync();

        var responseEntities = semesters
            .Select(s => new SemesterIdSelectsEventResponseEntity
            {
                SemesterNumber = s.SemesterNumber,
                SemesterId = s.SemesterId
            })
            .ToList();

        var response = new SemesterIdSelectsEventResponse
        {
            Response = responseEntities
        };

        await context.RespondAsync(response);
    }
}