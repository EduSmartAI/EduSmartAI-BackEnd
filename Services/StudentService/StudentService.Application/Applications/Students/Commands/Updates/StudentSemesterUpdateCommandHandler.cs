using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Applications.Students.Commands.Updates;

public class StudentSemesterUpdateCommandHandler : ICommandHandler<StudentSemesterUpdateCommand, StudentSemesterUpdateCommandResponse>
{
    private readonly ICommandRepository<Student>  _studentRepository;
    private readonly IQueryRepository<StudentCollection>  _studentCollectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService  _identityService;
    private readonly IRequestClient<MajorAndSemesterSelectEvent> _requestClientMajorAndSemesterSelect;

    public StudentSemesterUpdateCommandHandler(ICommandRepository<Student> studentRepository, IQueryRepository<StudentCollection> studentCollectionRepository, IUnitOfWork unitOfWork, IIdentityService identityService, IRequestClient<MajorAndSemesterSelectEvent> requestClientMajorAndSemesterSelect)
    {
        _studentRepository = studentRepository;
        _studentCollectionRepository = studentCollectionRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _requestClientMajorAndSemesterSelect = requestClientMajorAndSemesterSelect;
    }

    public async Task<StudentSemesterUpdateCommandResponse> Handle(StudentSemesterUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentSemesterUpdateCommandResponse { Success = false };

        var student = await _studentRepository.FirstOrDefaultAsync(x =>
            x.StudentId == _identityService.GetCurrentUser()!.UserId &&
            x.IsActive, cancellationToken);
        
        var responseMajorAndSemester = await _requestClientMajorAndSemesterSelect
            .GetResponse<MajorAndSemesterSelectEventResponse>(new MajorAndSemesterSelectEvent
            {
                MajorId = student!.MajorId,
                SemesterId = request.SemesterId
            }, cancellationToken);
        if (!responseMajorAndSemester.Message.Success)
        {
            response.SetMessage(MessageId.E00000, responseMajorAndSemester.Message.Message);
            return response;
        }
        var semesterName = responseMajorAndSemester.Message.Response.Semester!.SemesterName;

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            student.SemesterId = request.SemesterId;

            var studentCollection = await _studentCollectionRepository
                .FirstOrDefaultAsync(x => x.StudentId == student.StudentId && x.IsActive);
            
            studentCollection!.SemesterId = request.SemesterId;
            studentCollection.SemesterName = semesterName;
            
            _studentRepository.Update(student);
            _unitOfWork.Store(studentCollection);
            await _unitOfWork.SaveChangesAsync(_identityService.GetCurrentUser()!.Email, cancellationToken);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật kỳ học");
            return true;
        }, cancellationToken);
        
        
        return response;
    }
}