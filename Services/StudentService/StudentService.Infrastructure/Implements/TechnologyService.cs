using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using StudentService.Application.Applications.LearningGoals.Queries;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class TechnologyService(ICommandRepository<Technology> technologyRepository,
    IUnitOfWork unitOfWork,
    IQueryRepository<TechnologyCollection> technologyQueryRepository,
    IIdentityService identityService) : ITechnologyService
{
    /// <summary>
    /// Insert new technology
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TechnologyInsertResponse> InsertTechnologyAsync(TechnologyInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new TechnologyInsertResponse { Success = false };
        
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            var technology = new Technology
            {
                TechnologyName = request.TechnologyName,
                Description = request.Description,
                TechnologyType = request.TechnologyType,
            };

            await technologyRepository.AddAsync(technology);
            await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, cancellationToken);

            // Add to read model
            unitOfWork.Store(TechnologyCollection.FromWriteModel(technology));
            await unitOfWork.SessionSaveChangesAsync();
            await unitOfWork.CacheRemoveAsync("technologies_all");
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm mới công nghệ thành công");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Select technologies
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<TechnologySelectsEventResponse> SelectTechnologiesAsync(TechnologySelectsQuery request)
    {
        var response = new TechnologySelectsEventResponse { Success = false };
        
        string cacheKey = $"technologies_all";

        var query = await technologyQueryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                return await technologyQueryRepository
                    .ToListAsync(x => x.IsActive && 
                                      (x.TechnologyType == (short) ConstantEnum.TechnologyType.ProgrammingLanguage ||
                                      x.TechnologyType == (short) ConstantEnum.TechnologyType.Framework));
            },
            TimeSpan.FromMinutes(10));
        if (!query.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy công nghệ nào");
            return response;
        }
        
        // Set response
        response.Response = query.Select(x => new TechnologySelectsEventResponseEntity
        {
            TechnologyId = x.TechnologyId,
            TechnologyName = x.TechnologyName,
            TechnologyType = x.TechnologyType
        }).ToList();
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy danh sách công nghệ");
        return response;
    }
}