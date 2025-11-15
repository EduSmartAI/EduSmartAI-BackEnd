using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using StudentService.Application.Applications.Students.Queries.Logins;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.Students.Queries.Logins;

public class StudentLoginQueryHandler : IQueryHandler<StudentLoginQuery, StudentLoginEventResponse>
{
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQueryRepository"></param>
    public StudentLoginQueryHandler(IQueryRepository<StudentCollection> studentQueryRepository)
    {
        _studentQueryRepository = studentQueryRepository;
    }

    /// <summary>
    /// Handle user login
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentLoginEventResponse> Handle(StudentLoginQuery request, CancellationToken cancellationToken)
    {
        var response = new StudentLoginEventResponse { Success = false };

        // Check if the user is a student
        var student = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == request.UserId);
        if (student == null)
        {
            response.SetMessage(MessageId.E11005);
            return response;
        }

        // Set response entity
        response.Response = new UserLoginEntity
        (
            FirstName: student.FirstName!,
            LastName: student.LastName!,
            AvatarUrl: student.AvatarUrl
        );
            
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Đăng nhập");
        return response;
    }
}