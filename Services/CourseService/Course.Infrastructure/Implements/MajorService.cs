using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Majors.Queries;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class MajorService(ICommandRepository<Major> commandRepository) : IMajorService
{
    /// <summary>
    /// Select MajorName by Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SelectMajorAsync(Guid id, CancellationToken cancellationToken) 
        => await commandRepository.FirstOrDefaultAsync(x => x.MajorId == id, cancellationToken).Select(x => x.MajorName);

    /// <summary>
    /// Select all majors
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MajorSelectsEventResponse> SelectMajorsAsync(MajorSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new MajorSelectsEventResponse { Success = false };
        
        // Get data
        var majorsQuery = commandRepository.Find(x => x.IsActive, isTracking: false);

        if (request.MajorCodes != null && request.MajorCodes.Any())
        {
            majorsQuery = majorsQuery.Where(x => request.MajorCodes.Contains(x.MajorCode));
        }

        var query = await majorsQuery
            .Select(x => new MajorSelectsEventResponseEntity
            {
                MajorId = x.MajorId,
                MajorName = x.MajorName,
                MajorCode = x.MajorCode,
                ParentMajorId = x.ParentMajorId
            })
            .ToListAsync(cancellationToken: cancellationToken);

        if (!query.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy chuyên ngành nào");
            return response;
        }        
        // True
        response.Success = true;
        response.Response = query;
        response.SetMessage(MessageId.I00001, "Lấy danh sách chuyên ngành");
        return response;
    }
}