using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;
using TeacherService.Domain.ReadModels;

namespace TeacherService.Application.Applications.Teachers.Queries.Logins;

public class TeacherLoginQueryHandler : IQueryHandler<TeacherLoginQuery, TeacherLoginEventResponse>
{
    private readonly IQueryRepository<TeacherCollection> _teacherQueryRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="teacherQueryRepository"></param>
    public TeacherLoginQueryHandler(IQueryRepository<TeacherCollection> teacherQueryRepository)
    {
        _teacherQueryRepository = teacherQueryRepository;
    }

    /// <summary>
    /// Handle teacher login
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TeacherLoginEventResponse> Handle(TeacherLoginQuery request, CancellationToken cancellationToken)
    {
        var response = new TeacherLoginEventResponse { Success = false };

        // Check if the user is a teacher
        var teacher = await _teacherQueryRepository.FirstOrDefaultAsync(x => x.TeacherId == request.UserId);
        if (teacher == null)
        {
            response.SetMessage(MessageId.E11005);
            return response;
        }

        // Set response entity
        response.Response = new UserLoginEntity
        (
            FirstName: teacher.FirstName!,
            LastName: teacher.LastName!,
            AvatarUrl: teacher.ProfilePictureUrl
        );
            
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Đăng nhập");
        return response;
    }
}
