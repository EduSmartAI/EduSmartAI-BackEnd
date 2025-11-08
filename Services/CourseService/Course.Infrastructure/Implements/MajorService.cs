using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Majors.Commands.CreateMajor;
using Course.Application.Majors.Queries;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class MajorService(
    IIdentityService _identityService,
    IUnitOfWork unitOfWork,
    ICommandRepository<Major> _commandRepository) : IMajorService
{
    /// <summary>
    /// Create Major
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CreateMajorResponse> CreateMajorAsync(CreateMajorCommand request, CancellationToken cancellationToken)
    {
        var response = new CreateMajorResponse { Success = false };
        var userEmail = _identityService.GetCurrentUser()!.Email;

        var dto = request.CreateMajorDto;
        var code = dto.MajorCode?.Trim().ToUpperInvariant();
        var name = dto.MajorName?.Trim();
        var description = TrimOrNull(dto.Description);

        var existed = await _commandRepository.FirstOrDefaultAsync(
            m => m.MajorCode.ToUpper() == code,
            cancellationToken
        );

        if (existed is not null)
        {
            response.SetMessage(MessageId.E00000, $"Chuyên ngành với mã '{code}' đã tồn tại.");
            return response;
        }

        var parentMajor = await _commandRepository.FirstOrDefaultAsync(
            m => m.MajorCode.ToUpper() == "SE",
            cancellationToken
        );

        if (parentMajor is null)
        {
            response.SetMessage(MessageId.E00000, "Chuyên ngành cha với mã 'SE' không tồn tại.");
            return response;
        }

        var entity = new Major
        {
            MajorId = Guid.NewGuid(),
            MajorCode = code,
            MajorName = name,
            Description = description,
            ParentMajorId = parentMajor.MajorId,
        };

        await _commandRepository.AddAsync(entity, userEmail);
        await unitOfWork.SaveChangesAsync(userEmail, cancellationToken);

        response.Success = true;
        response.Response = true;
        response.SetMessage(MessageId.I00001, "Tạo chuyên ngành thành công.");

        return response;
    }

    /// <summary>
    /// Select MajorName by Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SelectMajorAsync(Guid id, CancellationToken cancellationToken)
        => await _commandRepository.FirstOrDefaultAsync(x => x.MajorId == id, cancellationToken).Select(x => x.MajorName);

    /// <summary>
    /// Select all majors
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MajorSelectsEventResponse> SelectMajorsAsync(MajorCodeSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new MajorSelectsEventResponse { Success = false };

        // Get data
        var majorsQuery = _commandRepository.Find(x => x.IsActive, isTracking: false);

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

    #region Private Helper Methods

    /// <summary>
    /// Helper : Trim string or return null if empty
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static string? TrimOrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    #endregion
}