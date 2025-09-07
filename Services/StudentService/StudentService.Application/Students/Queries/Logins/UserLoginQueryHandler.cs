using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Students.Queries.Logins;

public class UserLoginQueryHandler : IQueryHandler<UserLoginQuery, UserLoginEventResponse>
{
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly IQueryRepository<TeacherCollection> _teacherCollectionQueryRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQueryRepository"></param>
    /// <param name="teacherCollectionQueryRepository"></param>
    public UserLoginQueryHandler(IQueryRepository<StudentCollection> studentQueryRepository, IQueryRepository<TeacherCollection> teacherCollectionQueryRepository)
    {
        _studentQueryRepository = studentQueryRepository;
        _teacherCollectionQueryRepository = teacherCollectionQueryRepository;
    }

    public async Task<UserLoginEventResponse> Handle(UserLoginQuery request, CancellationToken cancellationToken)
    {
        var response = new UserLoginEventResponse { Success = false };

        // Check if the user is a student
        var student = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == request.UserId);
        if (student != null)
        {
            response.Response = new UserLoginEntity
            (
                FirstName: student.FirstName!,
                LastName: student.LastName!
            );
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng nhập");
            return response;
        }

        // Check if the user is a teacher
        var teacher = await _teacherCollectionQueryRepository.FirstOrDefaultAsync(x => x.TeacherId == request.UserId);
        if (teacher != null)
        {
            response.Response = new UserLoginEntity
            (
                FirstName: teacher.FirstName!,
                LastName: teacher.LastName!
            );
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng nhập");
            return response;
        }

        // If neither student nor teacher, return false
        response.SetMessage(MessageId.E00000, "User not found or not authorized.");
        return response;
    }
}