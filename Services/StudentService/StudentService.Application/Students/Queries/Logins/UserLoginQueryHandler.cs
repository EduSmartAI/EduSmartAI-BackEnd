using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Students.Queries.Logins;

public class UserLoginQueryHandler : IQueryHandler<UserLoginQuery, UserLoginEventResponse>
{
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQueryRepository"></param>
    public UserLoginQueryHandler(IQueryRepository<StudentCollection> studentQueryRepository)
    {
        _studentQueryRepository = studentQueryRepository;
    }

    /// <summary>
    /// Handle user login
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UserLoginEventResponse> Handle(UserLoginQuery request, CancellationToken cancellationToken)
    {
        var response = new UserLoginEventResponse { Success = false };

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
            LastName: student.LastName!
        );
            
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Đăng nhập");
        return response;
    }
}