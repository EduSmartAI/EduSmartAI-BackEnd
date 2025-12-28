using BaseService.Application.Common;
using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;
using Course.Application.DTOs.SyllabusDTO.Semester;
using Course.Application.Semesters.Queries.GetSemesterDetails;
using Course.Application.Semesters.Queries.GetSemesters;
using Course.Application.Semesters.Queries.SelectSemesters;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class SemesterService(ICommandRepository<Semester> _semesterCommandRepository) : ISemesterService
{
	/// <summary>
	/// Get Semester Detail
	/// </summary>
	/// <param name="semesterId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<GetSemesterDetailResponse> GetSemesterDetailAsync(Guid semesterId, CancellationToken ct = default)
	{
		var response = new GetSemesterDetailResponse { Success = false };

		var entity = await _semesterCommandRepository.FirstOrDefaultAsync(x => x.SemesterId == semesterId, ct);
		if (entity is null)
		{
			response.SetMessage(MessageId.E00000, "Không tìm thấy học kỳ");
			return response;
		}

		response.Response = new SemesterDto(
			entity.SemesterId,
			entity.SemesterCode,
			entity.SemesterName,
			entity.SemesterNumber
		);

		response.Success = true;
		response.SetMessage(MessageId.I00001, $"Lấy thông tin kỳ {entity.SemesterNumber}");

		return response;
	}

	/// <summary>
	/// Get Semesters
	/// </summary>
	/// <param name="page"></param>
	/// <param name="size"></param>
	/// <param name="search"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<GetSemestersResponse> GetSemestersAsync(int? page, int? size, string? search, CancellationToken ct = default)
	{
		var response = new GetSemestersResponse { Success = false };

		Expression<Func<Semester, bool>>? predicate = null;
		if (!string.IsNullOrWhiteSpace(search))
		{
			var s = search.Trim().ToLower();
			predicate = x =>
				x.SemesterCode.ToLower().Contains(s) ||
				x.SemesterName.ToLower().Contains(s);
		}

		var paged = await _semesterCommandRepository.PagedAsync(
			pageNumber: page,
			pageSize: size,
			predicate: predicate,
			orderBy: x => x.SemesterNumber,
			orderByDescending: false,
			cancellationToken: ct
		);

		var dtoItems = paged.Items.Select(x =>
			new SemesterDto(
				x.SemesterId,
				x.SemesterCode,
				x.SemesterName,
				x.SemesterNumber
			)).ToList();

		var dtoPaged = new PagedResult<SemesterDto>
		{
			Items = dtoItems,
			TotalCount = paged.TotalCount,
			PageNumber = paged.PageNumber,
			PageSize = paged.PageSize
		};

		response.Response = dtoPaged;
		response.Success = true;
		response.SetMessage(MessageId.I00001, "Lấy thông tin tất cả kỳ học");

		return response;
	}

	/// <summary>
	/// Select SemesterName by Id
	/// </summary>
	/// <param name="id"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<Semester?> SelectSemesterAsync(Guid id, CancellationToken cancellationToken)
        => await _semesterCommandRepository.FirstOrDefaultAsync(x => x.SemesterId == id, cancellationToken);
    
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
        var query = await _semesterCommandRepository
            .Find(isTracking: false)
            .Select(x => new SemesterSelectsEventResponseEntity
            {
                SemesterId = x.SemesterId,
                SemesterName = x.SemesterName,
                SemesterNumber = x.SemesterNumber
            })
            .OrderBy(x => x.SemesterNumber)
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