using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Students.Queries.SelectUserProfile;

public class UserProfileSelectQueryHandler : IQueryHandler<UserProfileSelectQuery, StudentProfileSelectResponse>
{
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly IIdentityService _identityService;

    public UserProfileSelectQueryHandler(IQueryRepository<StudentCollection> studentQueryRepository,
        IIdentityService identityService)
    {
        _studentQueryRepository = studentQueryRepository;
        _identityService = identityService;
    }

    public async Task<StudentProfileSelectResponse> Handle(UserProfileSelectQuery request, CancellationToken cancellationToken)
    {
        // var response = new StudentProfileSelectResponse { Success = false };
        //
        // // Get current user by token
        // var currentUser = _identityService.GetCurrentUser();
        // var entityResponse = new StudentProfileSelectEntity();
        //
        // // Select student by user id
        // var student = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == currentUser!.UserId);
        // if (student == null)
        // {
        //     response.SetMessage(MessageId.E00000, CommonMessages.StudentNotFound);
        //     return response;
        // }
        //
        // var studentEntity = new StudentProfile
        // {
        //     StudentId = student.StudentId,
        //     FirstName = student.FirstName,
        //     LastName = student.LastName,
        //     Email = currentUser!.Email,
        //     PhoneNumber = student.PhoneNumber,
        //     AvatarUrl = student.AvatarUrl,
        //     DateOfBirth = student.DateOfBirth,
        //     Gender = student.Gender,
        //     Address = student.Address,
        //     Bio = student.Bio,
        //     Marjor = student.Marjor,
        //     SkillLevel = student.SkillLevel
        // };
        //
        // // Set response entity
        // entityResponse.StudentProfile = studentEntity;
        //
        // // Return response
        // response.Success = true;
        // response.Response = entityResponse;
        // return response;
        return null;
    }
}