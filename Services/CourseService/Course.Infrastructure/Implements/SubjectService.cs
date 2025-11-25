using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Subjects.Commands.CreateSubject;
using Course.Application.Subjects.Queries;

namespace Course.Infrastructure.Implements;

public class SubjectService(
    ICommandRepository<Subject> subjectRepository, 
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

        var existed = await subjectRepository.FirstOrDefaultAsync(
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
			var prereqSubjects = await subjectRepository
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

		await subjectRepository.AddAsync(entity, userEmail);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        response.Success = true;
        response.Response = true;
        response.SetMessage(MessageId.I00001, "Tạo môn học");

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
        
       var subjectSelects = await subjectRepository
            .Find(x => request.SubjectIds.Contains(x.SubjectId) && x.IsActive, cancellationToken: cancellationToken)
            .Select(x => new SubjectSelectEventResponseEntity
            {
                SubjectId = x.SubjectId,
                SubjectName = $"{x.SubjectName} - {x.SubjectCode}",
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