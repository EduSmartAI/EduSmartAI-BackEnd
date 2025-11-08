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

        var entity = new Subject
        {
            SubjectCode = subjectCode,
            SubjectName = subjectName,
        };

        await subjectRepository.AddAsync(entity, userEmail);
        await unitOfWork.SaveChangesAsync(userEmail, cancellationToken);

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