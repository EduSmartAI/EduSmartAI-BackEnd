using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
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
            
            // Update Learning Goal - if not exist, add new list
            if (message.StudentLearningGoal != null)
            {
                var learningGoalExist = student.LearningGoals?.Any(x => x.GoalId == message.StudentLearningGoal.GoalId);
                if (learningGoalExist != true)
                {
                    if (student.LearningGoals == null)
                    {
                        student.LearningGoals = new List<StudentLearningGoalCollection>();
                    }
                    student.LearningGoals.Add(message.StudentLearningGoal);
                }
            }
            
            // Update Technologies - always replace with latest from event
            if (message.StudentTechnologies != null && message.StudentTechnologies.Any())
            {
                student.Technologies = message.StudentTechnologies.ToList();
            }
            else
            {
                student.Technologies = new List<StudentTechnologyCollection>();
            }
            
            unitOfWork.Store(student);
        }

        // Store all technologies from event
        if (message.StudentTechnologies != null && message.StudentTechnologies.Any())
        {
            foreach (var tech in message.StudentTechnologies)
            {
                unitOfWork.Store(tech);
            }
        }

        // Store learning goal from event
        if (message.StudentLearningGoal != null)
        {
            unitOfWork.Store(message.StudentLearningGoal);
        }
        await unitOfWork.CacheRemoveAsync(CacheKey.StudentProfile(message.Student.StudentId));
        await unitOfWork.SessionSaveChangesAsync();
    }
}