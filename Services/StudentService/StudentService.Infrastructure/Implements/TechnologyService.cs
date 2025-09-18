using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class TechnologyService(ICommandRepository<Technology> technologyRepository,
    IUnitOfWork unitOfWork,
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
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm mới công nghệ thành công");
            return true;
        }, cancellationToken);
        return response;
    }
}