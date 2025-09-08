using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Students.Queries.SelectUserProfile;

public class UserProfileSelectQueryHandler : IQueryHandler<UserProfileSelectQuery, UserProfileSelectResponse>
{
    private readonly IQueryRepository<TeacherCollection> _teacherQueryRepository;
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly IIdentityService _identityService;

    public UserProfileSelectQueryHandler(IQueryRepository<TeacherCollection> teacherQueryRepository,
        IQueryRepository<StudentCollection> studentQueryRepository, IIdentityService identityService)
    {
        _teacherQueryRepository = teacherQueryRepository;
        _studentQueryRepository = studentQueryRepository;
        _identityService = identityService;
    }

    public async Task<UserProfileSelectResponse> Handle(UserProfileSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new UserProfileSelectResponse { Success = false };

        // Get current user by token
        var currentUser = _identityService.GetCurrentUser();
        var entityResponse = new UserProfileSelectEntity();

        // If the user is a teacher, get the teacher profile
        if (currentUser!.RoleName == nameof(ConstantEnum.UserRole.Lecturer))
        {
            // Select lecturer by user id
            var lecturer = await _teacherQueryRepository.FirstOrDefaultAsync(x => x.TeacherId == currentUser.UserId);
            if (lecturer == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.LecturerNotFound);
                return response;
            }

            var lecturerEntity = new LecturerProfile
            {
                LecturerId = lecturer.TeacherId,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
                Email = currentUser.Email,
                PhoneNumber = lecturer.PhoneNumber,
                AvatarUrl = lecturer.AvatarUrl,
                DateOfBirth = lecturer.DateOfBirth,
                Gender = lecturer.Gender,
                Address = lecturer.Address,
                Bio = lecturer.Bio,
                Marjor = lecturer.Marjor,
                Degree = lecturer.Degree,
                Specialization = lecturer.Specialization,
                TeachingExperienceYears = lecturer.TeachingExperienceYears,
                IsVerified = lecturer.IsVerified
            };

            // Set response entity
            entityResponse.TeacherProfile = lecturerEntity;
        }

        else if (currentUser.RoleName == nameof(ConstantEnum.UserRole.Student))
        {
            // Select student by user id
            var student = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == currentUser.UserId);
            if (student == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.StudentNotFound);
                return response;
            }

            var studentEntity = new StudentProfile
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = currentUser.Email,
                PhoneNumber = student.PhoneNumber,
                AvatarUrl = student.AvatarUrl,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Address = student.Address,
                Bio = student.Bio,
                Marjor = student.Marjor,
                SkillLevel = student.SkillLevel
            };

            // Set response entity
            entityResponse.StudentProfile = studentEntity;
        }

        // Return response
        response.Success = true;
        response.Response = entityResponse;
        return response;
    }
}