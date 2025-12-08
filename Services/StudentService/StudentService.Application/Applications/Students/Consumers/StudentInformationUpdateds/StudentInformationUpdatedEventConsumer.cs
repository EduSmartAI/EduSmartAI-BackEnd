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
                if (student.LearningGoals == null || !student.LearningGoals.Any())
                {
                    student.LearningGoals = new List<StudentLearningGoalCollection>();
                    student.LearningGoals.Add(message.StudentLearningGoal);
                }
                var learningGoalExist = student.LearningGoals?.FirstOrDefault(x => x.GoalId == message.StudentLearningGoal.GoalId && x.IsActive);
                if (learningGoalExist != null)
                {
                    student.LearningGoals!.Remove(learningGoalExist);
                    student.LearningGoals.Add(message.StudentLearningGoal);
                }
                unitOfWork.Store(student);
            }
            
            // Update Technologies - always replace with latest from event
            if (message.StudentTechnologies != null && message.StudentTechnologies.Any())
            {
                if (student.Technologies == null || !student.Technologies.Any())
                {
                    student.Technologies = new List<StudentTechnologyCollection>();
                    foreach (var technology in message.StudentTechnologies)
                    {
                        student.Technologies.Add(technology);
                    }
                }
                else
                {
                    student.Technologies = message.StudentTechnologies.ToList();
                }
                unitOfWork.Store(student);
            }
        }
        await unitOfWork.CacheRemoveAsync(CacheKey.StudentProfile(message.Student.StudentId));
        await unitOfWork.SessionSaveChangesAsync();
    }
}