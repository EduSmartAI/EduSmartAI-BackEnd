using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using StudentService.Application.Applications.LearningGoals.Queries;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Applications.Technologies.Queries;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class TechnologyService(ICommandRepository<Technology> technologyRepository, IUnitOfWork unitOfWork, IQueryRepository<TechnologyCollection> technologyQueryRepository, IIdentityService identityService) : ITechnologyService
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
                TechnologyType = (short) request.TechnologyType,
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
    /// Update existing technology
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TechnologyUpdateResponse> UpdateTechnologyAsync(TechnologyUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new TechnologyUpdateResponse { Success = false };
        
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check if technology exists
            var technology = await technologyRepository.FirstOrDefaultAsync(x => x.TechnologyId == request.TechnologyId, cancellationToken);
            if (technology == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy công nghệ");
                return false;
            }
            
            // Update technology
            technology.TechnologyName = request.TechnologyName;
            technology.Description = request.Description;
            technology.TechnologyType = (short) request.TechnologyType;
            
            technologyRepository.Update(technology);
            await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, cancellationToken);

            // Update read model
            var technologyCollection = await technologyQueryRepository.FirstOrDefaultAsync(x => x.TechnologyId == request.TechnologyId);
            if (technologyCollection != null)
            {
                technologyCollection.TechnologyName = request.TechnologyName;
                technologyCollection.Description = request.Description;
                technologyCollection.TechnologyType = (short) request.TechnologyType;
                technologyCollection.UpdatedAt = technology.UpdatedAt;
                technologyCollection.UpdatedBy = technology.UpdatedBy;
                
                unitOfWork.Store(technologyCollection);
                await unitOfWork.SessionSaveChangesAsync();
            }
            
            await unitOfWork.CacheRemoveAsync("technologies_all");
            
            // True
            response.Success = true;
            response.Response = "Cập nhật công nghệ thành công";
            response.SetMessage(MessageId.I00001, "Cập nhật công nghệ thành công");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Delete technology (logical delete)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TechnologyDeleteResponse> DeleteTechnologyAsync(TechnologyDeleteCommand request, CancellationToken cancellationToken)
    {
        var response = new TechnologyDeleteResponse { Success = false };
        
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check if technology exists
            var technology = await technologyRepository.FirstOrDefaultAsync(x => x.TechnologyId == request.TechnologyId, cancellationToken);
            if (technology == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy công nghệ");
                return false;
            }
            
            // Logical delete
            technologyRepository.Update(technology);
            await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, cancellationToken, needLogicalDelete: true);

            // Delete from read model
            var technologyCollection = await technologyQueryRepository.FirstOrDefaultAsync(x => x.TechnologyId == request.TechnologyId);
            if (technologyCollection != null)
            {
                unitOfWork.Delete(technologyCollection);
                await unitOfWork.SessionSaveChangesAsync();
            }
            
            await unitOfWork.CacheRemoveAsync("technologies_all");
            
            // True
            response.Success = true;
            response.Response = "Xóa công nghệ thành công";
            response.SetMessage(MessageId.I00001, "Xóa công nghệ thành công");
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

    public async Task<AdminTechnologiesSelectResponse> SelectAdminTechnologiesAsync(AdminTechnologiesSelectQuery request)
    {
        var response = new AdminTechnologiesSelectResponse { Success = false };
    
        // Get all technologies
        var query = await technologyQueryRepository.ToListAsync();
    
        // Filter by search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x =>
                x.TechnologyName.Contains(request.SearchTerm) ||
                (x.Description != null && x.Description.Contains(request.SearchTerm))
            ).ToList();
        }
    
        // Filter by technology type
        if (request.TechnologyType.HasValue)
        {
            query = query.Where(x => x.TechnologyType == request.TechnologyType.Value).ToList();
        }
    
        var totalCount = query.Count;
    
        // Pagination
        var technologies = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
    
        if (!technologies.Any() && request.PageNumber == 1)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy công nghệ nào");
            return response;
        }
    
        // Map to response DTO
        var items = technologies.Select(x => new AdminTechnologyItem
        {
            TechnologyId = x.TechnologyId,
            TechnologyName = x.TechnologyName,
            Description = x.Description,
            TechnologyType = x.TechnologyType,
            CreatedAt = x.CreatedAt,
            TechnologyTypeName = GetTechnologyTypeName(x.TechnologyType)
        }).ToList();
    
        response.Response = new PagedResult<AdminTechnologyItem>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy danh sách công nghệ");
        return response;
    }
    
    // Helper method to get technology type name
    private string GetTechnologyTypeName(short technologyType)
    {
        return technologyType switch
        {
            (short) ConstantEnum.TechnologyType.ProgrammingLanguage => "Programming Language",
            (short) ConstantEnum.TechnologyType.Framework => "Framework",
        };
    }
}