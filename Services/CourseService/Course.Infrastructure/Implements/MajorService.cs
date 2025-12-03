using BaseService.Application.Common;
using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.DTOs.SyllabusDTO.Majors;
using Course.Application.Majors.Commands.CreateMajor;
using Course.Application.Majors.Queries.GetMajorDetails;
using Course.Application.Majors.Queries.GetMajors;
using Course.Application.Majors.Queries.SelectMajorCode;
using Course.Infrastructure.Extensions;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class MajorService(
	IIdentityService identityService,
	IUnitOfWork unitOfWork,
	ICommandRepository<Major> majorCommandRepository) : IMajorService
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
		var userEmail = identityService.GetCurrentUser()!.Email;

		var dto = request.CreateMajorDto;
		var code = dto.MajorCode?.Trim().ToUpperInvariant();
		var name = dto.MajorName?.Trim();
		var description = TrimOrNull(dto.Description);

		var existed = await majorCommandRepository.FirstOrDefaultAsync(
			m => m.MajorCode.ToUpper() == code,
			cancellationToken
		);

		if (existed is not null)
		{
			response.SetMessage(MessageId.E00000, $"Chuyên ngành với mã '{code}' đã tồn tại.");
			return response;
		}

		var parentMajorCode = "SE";

		var parentMajor = await majorCommandRepository.FirstOrDefaultAsync(
			m => m.MajorCode.ToUpper() == parentMajorCode,
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
			RequiredCredits = dto.RequiredCredits,
		};

		await majorCommandRepository.AddAsync(entity, userEmail);
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
	public async Task<Major?> SelectMajorAsync(Guid id, CancellationToken cancellationToken)
		=> await majorCommandRepository.FirstOrDefaultAsync(x => x.MajorId == id, cancellationToken);

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
		var majorsQuery = majorCommandRepository.Find(x => x.IsActive, isTracking: false);

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

	/// <summary>
	/// Get Majors with paging and searching
	/// </summary>
	/// <param name="page"></param>
	/// <param name="size"></param>
	/// <param name="search"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<GetMajorsResponse> GetMajorsAsync(int? page, int? size, string? search, CancellationToken ct = default)
	{
		var response = new GetMajorsResponse { Success = false };

		Expression<Func<Major, bool>>? predicate = x => x.IsActive;

		if (!string.IsNullOrWhiteSpace(search))
		{
			var s = search.Trim().ToLower();
			predicate = predicate == null
				? x => x.MajorCode.ToLower().Contains(s) || x.MajorName.ToLower().Contains(s)
				: predicate.AndAlso(x => x.MajorCode.ToLower().Contains(s) || x.MajorName.ToLower().Contains(s));
		}

		var paged = await majorCommandRepository.PagedAsync(
			pageNumber: page,
			pageSize: size,
			predicate: predicate,
			orderBy: x => x.MajorName,
			orderByDescending: false,
			cancellationToken: ct
		);

		var dtoItems = paged.Items.Select(x =>
			new MajorDto(
				x.MajorId,
				x.MajorCode,
				x.MajorName,
				x.Description,
				x.RequiredCredits
			)).ToList();

		var dtoPaged = new PagedResult<MajorDto>
		{
			Items = dtoItems,
			TotalCount = paged.TotalCount,
			PageNumber = paged.PageNumber,
			PageSize = paged.PageSize
		};

		response.Response = dtoPaged;
		response.Success = true;
		response.SetMessage(MessageId.I00001, "Lấy tất cả các chuyên ngành (Major)");

		return response;
	}

	/// <summary>
	/// Get Major Detail
	/// </summary>
	/// <param name="majorId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<GetMajorDetailResponse> GetMajorDetailAsync(Guid majorId, CancellationToken ct = default)
	{
		var response = new GetMajorDetailResponse { Success = false };

		var entity = await majorCommandRepository.FirstOrDefaultAsync(x => x.MajorId == majorId, ct);
		if (entity is null)
		{
			response.SetMessage(MessageId.E00000, "Không tìm thấy ngành học");
			return response;
		}

		response.Response = new MajorDto(
			entity.MajorId,
			entity.MajorCode,
			entity.MajorName,
			entity.Description,
			entity.RequiredCredits
		);
		response.Success = true;
		response.SetMessage(MessageId.I00001, $"Lấy thông tin ngành {entity.MajorCode}");

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