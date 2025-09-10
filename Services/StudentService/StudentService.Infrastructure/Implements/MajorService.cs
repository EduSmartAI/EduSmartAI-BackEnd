using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using StudentService.Application.Applications.Majors.Commands;
using StudentService.Application.Applications.Majors.Queries;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class MajorService : IMajorService
{
    private readonly ICommandRepository<Major> _majorCommandRepository;
    private readonly IQueryRepository<MajorCollection> _majorQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="majorCommandRepository"></param>
    /// <param name="majorQueryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="identityService"></param>
    public MajorService(ICommandRepository<Major> majorCommandRepository, IQueryRepository<MajorCollection> majorQueryRepository, IUnitOfWork unitOfWork, IIdentityService identityService)
    {
        _majorCommandRepository = majorCommandRepository;
        _majorQueryRepository = majorQueryRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
    }

    /// <summary>
    /// Insert new major
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MajorInsertResponse> InsertMajorAsync(MajorInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new MajorInsertResponse { Success = false};

        var currentUserEmail = _identityService.GetCurrentUser()!.Email;
        
        // Check if major already exists
        var majorExist = await _majorCommandRepository.FirstOrDefaultAsync(x => x.MajorName == request.MajorName || x.MajorCode == request.MarjorCode, cancellationToken);
        if (majorExist != null)
        {
            response.SetMessage(MessageId.E00000, "Chuyên ngành đã tồn tại");
            return response;
        }
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new major
            var major = new Major
            {
                MajorName = request.MajorName,
                MajorCode = request.MarjorCode,
                Description = request.Description,
            };
            
            // Add to repository
            await _majorCommandRepository.AddAsync(major);
            await _unitOfWork.SaveChangesAsync(currentUserEmail, cancellationToken);

            _unitOfWork.Store(MajorCollection.FromWriteModel(major));
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm chuyên ngành");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Select majors
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<MajorsSelectResponse> SelectMajorsAsync(MajorsSelectQuery request)
    {
        var response = new MajorsSelectResponse {Success = false};
        
        string cacheKey = "majors:all";
        
        // Get majors from cache or database
        var majors = await _majorQueryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _majorQueryRepository.ToListAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (!majors.Any())
        {
            response.SetMessage(MessageId.E00000, CommonMessages.MajorsNotFound);
            return response;
        }
        
        var responseEntity = majors.Select(x => new MajorsSelectResponseEntity
        {
            MajorId = x.MajorId,
            MajorName = x.MajorName,
            MajorCode = x.MajorCode,
            Description = x.Description
        }).ToList();
        
        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy danh sách chuyên ngành");
        return response;
    }
}