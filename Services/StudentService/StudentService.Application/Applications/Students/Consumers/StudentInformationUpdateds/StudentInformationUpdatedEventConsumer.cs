using BaseService.Application.Interfaces.Repositories;
using MassTransit;
using StudentService.Domain.ReadModels;

namespace StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;

public class StudentInformationUpdatedEventConsumer(IUnitOfWork unitOfWork, IQueryRepository<StudentCollection> studentRepository) : IConsumer<StudentInformationUpdatedEvent>
{
    public async Task Consume(ConsumeContext<StudentInformationUpdatedEvent> context)
    {
        var message = context.Message;

        // Update StudentCollection
        var student = await studentRepository.FirstOrDefaultAsync(x => x.StudentId == message.Student.StudentId);
        if (student != null)
        {
            student.MajorId = message.Student.MajorId;
            student.MajorName = message.Student.MajorName;
            student.SemesterId = message.Student.SemesterId;
            student.SemesterName = message.Student.SemesterName;
            student.LearningGoals = new List<StudentLearningGoalCollection> { message.StudentLearningGoal };        
        }
        unitOfWork.Store(student);
        
        foreach (var orientation in message.StudentOrientations)
        {
            unitOfWork.Store(orientation);
        }

        foreach (var tech in message.StudentTechnologies)
        {
            unitOfWork.Store(tech);
        }

        unitOfWork.Store(message.StudentLearningGoal);

        await unitOfWork.SessionSaveChangesAsync();
    }
}