using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Interfaces;
using Course.Application.Majors.Queries;
using Course.Domain.Models;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;

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
        var query = await commandRepository
            .Find(x => x.IsActive, isTracking: false)
            .Select(x => new MajorSelectsEventResponseEntity
            {
                MajorId = x.MajorId,
                MajorName = x.MajorName,
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