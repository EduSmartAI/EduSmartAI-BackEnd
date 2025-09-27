using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;
using Course.Application.Semesters.Queries;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class SemesterService(ICommandRepository<Semester> commandRepository) : ISemesterService
{
    /// <summary>
    /// Select SemesterName by Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SelectSemesterAsync(Guid id, CancellationToken cancellationToken)
        => await commandRepository.FirstOrDefaultAsync(x => x.SemesterId == id, cancellationToken).Select(x => x.SemesterName);
    
    /// <summary>
    /// Select all semesters
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SemesterSelectsEventResponse> SelectSemestersAsync(SemesterSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new SemesterSelectsEventResponse { Success = false };
        
        // Get data
        var query = await commandRepository
            .Find(isTracking: false)
            .Select(x => new SemesterSelectsEventResponseEntity
            {
                SemesterId = x.SemesterId,
                SemesterName = x.SemesterName,
                SemesterNumber = x.SemesterNumber
            })
            .ToListAsync(cancellationToken: cancellationToken);
        if (!query.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy học kỳ nào");
            return response;
        }
        
        // True
        response.Success = true;
        response.Response = query;
        response.SetMessage(MessageId.I00001, "Lấy danh sách học kỳ");
        return response;
    }
}