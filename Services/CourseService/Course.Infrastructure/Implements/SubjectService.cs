using BaseService.Application.Common;
using BuildingBlocks.Messaging.Events.QuizService;
using Course.Application.DTOs.SyllabusDTO.Subjects;
using Course.Application.Subjects.Commands.CreateSubject;
using Course.Application.Subjects.Queries.GetSubjectDetails;
using Course.Application.Subjects.Queries.GetSubjects;
using Course.Application.Subjects.Queries.SelectSubject;
using Course.Infrastructure.Extensions;

namespace Course.Infrastructure.Implements;

public class SubjectService(
    ICommandRepository<Subject> _subjectCommandRepository, 
    IUnitOfWork unitOfWork,
	IIdentityService _identityService) : ISubjectService
{
	/// <summary>
	/// Create Subject
	/// </summary>
	/// <param name="request"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<CreateSubjectResponse> CreateSubjectAsync(CreateSubjectCommand request, CancellationToken cancellationToken)
	{
		var response = new CreateSubjectResponse { Success = false };

        var userEmail = _identityService.GetCurrentUser()!.Email;

        var dto = request.CreateSubjectDto;
        var subjectCode = dto.SubjectCode?.Trim().ToUpperInvariant();
        var subjectName = dto.SubjectName?.Trim();

        var existed = await _subjectCommandRepository.FirstOrDefaultAsync(
            m => m.SubjectCode.ToUpper() == subjectCode,
            cancellationToken
        );

        if (existed is not null)
        {
            response.SetMessage(MessageId.E00000, $"Môn học với mã '{subjectCode}' đã tồn tại.");
            return response;
		}

		// 2. Chuẩn hóa list môn tiên quyết nếu có
		var prereqIds = dto.PrerequisiteSubjectIds?
			.Where(id => id != Guid.Empty)
			.Distinct()
			.ToList() ?? new List<Guid>();

		var entity = new Subject
        {
            SubjectCode = subjectCode,
            SubjectName = subjectName,
        };

		// 4. Nếu có môn tiên quyết thì load và gán
		if (prereqIds.Count > 0)
		{
			// Lấy toàn bộ subject tương ứng
			var prereqSubjects = await _subjectCommandRepository
				.Find(s => prereqIds.Contains(s.SubjectId), isTracking: true, cancellationToken)
				.ToListAsync(cancellationToken);

			// Kiểm tra xem có id nào không tồn tại không
			var foundIds = prereqSubjects.Select(s => s.SubjectId).ToHashSet();
			var missingIds = prereqIds.Where(id => !foundIds.Contains(id)).ToList();

			if (missingIds.Count > 0)
			{
				response.SetMessage(MessageId.E00000, "Một hoặc nhiều môn học ràng buộc không tồn tại.");
				return response;
			}

			// Gán danh sách môn tiên quyết cho môn hiện tại
			entity.PrereqSubjects = prereqSubjects;
		}

		await _subjectCommandRepository.AddAsync(entity, userEmail);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        response.Success = true;
        response.Response = true;
        response.SetMessage(MessageId.I00001, "Tạo môn học");

		return response;
	}

	/// <summary>
	/// Get Subject Details
	/// </summary>
	/// <param name="subjectId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<GetSubjectDetailResponse> GetSubjectDetailAsync(Guid subjectId, CancellationToken ct = default)
	{
		var response = new GetSubjectDetailResponse { Success = false };

		var entity = await _subjectCommandRepository.FirstOrDefaultAsync(x => x.SubjectId == subjectId, ct);
		if (entity is null)
		{
			response.SetMessage(MessageId.E00000, "Không tìm thấy môn học");
			return response;
		}

		response.Response = new SubjectDto(
			entity.SubjectId,
			entity.SubjectCode,
			entity.SubjectName
		);
		response.Success = true;
		response.SetMessage(MessageId.I00001, $"Lấy thông tin môn {entity.SubjectCode}");

		return response;
	}

	/// <summary>
	/// Get Subjects with paging and searching
	/// </summary>
	/// <param name="page"></param>
	/// <param name="size"></param>
	/// <param name="search"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<GetSubjectsResponse> GetSubjectsAsync(int? page, int? size, string? search, CancellationToken ct = default)
	{
		var response = new GetSubjectsResponse { Success = false };

		Expression<Func<Subject, bool>>? predicate = x => x.IsActive;

		if (!string.IsNullOrWhiteSpace(search))
		{
			var s = search.Trim().ToLower();
			predicate = predicate == null
				? x => x.SubjectCode.ToLower().Contains(s) || x.SubjectName.ToLower().Contains(s)
				: predicate.AndAlso(x => x.SubjectCode.ToLower().Contains(s) || x.SubjectName.ToLower().Contains(s));
		}

		var paged = await _subjectCommandRepository.PagedAsync(
			pageNumber: page,
			pageSize: size,
			predicate: predicate,
			orderBy: x => x.SubjectCode,
			orderByDescending: false,
			cancellationToken: ct
		);

		var dtoItems = paged.Items.Select(x =>
			new SubjectDto(
				x.SubjectId,
				x.SubjectCode,
				x.SubjectName
			)).ToList();

		var dtoPaged = new PagedResult<SubjectDto>
		{
			Items = dtoItems,
			TotalCount = paged.TotalCount,
			PageNumber = paged.PageNumber,
			PageSize = paged.PageSize
		};

		response.Response = dtoPaged;
		response.Success = true;
		response.SetMessage(MessageId.I00001, "OK");

		return response;
	}

	/// <summary>
	/// Select subjects
	/// </summary>
	/// <param name="request"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<SubjectSelectsEventResponse> SelectSubject(SubjectSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new SubjectSelectsEventResponse {Success = false};
        
       var subjectSelects = await _subjectCommandRepository
            .Find(x => request.SubjectIds.Contains(x.SubjectId) && x.IsActive, cancellationToken: cancellationToken)
            .Select(x => new SubjectSelectEventResponseEntity
            {
                SubjectId = x.SubjectId,
                SubjectNameCode = $"{x.SubjectName} - {x.SubjectCode}",
                SubjectName = x.SubjectName,
				SubjectCode = x.SubjectCode
            }).ToListAsync(cancellationToken: cancellationToken);
        
        if (subjectSelects.Count != request.SubjectIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy môn học phù hợp");
            return response;
        }

        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy danh sách môn học");
        response.Response = subjectSelects;
        return response;
    }
}