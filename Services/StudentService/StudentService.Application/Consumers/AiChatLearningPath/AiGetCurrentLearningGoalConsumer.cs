using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using MassTransit;
using StudentService.Domain.ReadModels;
using BaseService.Application.Interfaces.Repositories;

namespace StudentService.Application.Consumers.AiChatLearningPath;

public class AiGetCurrentLearningGoalConsumer(IQueryRepository<StudentCollection> studentRepository) : IConsumer<AiGetCurrentLearningGoal>
{
    public async Task Consume(ConsumeContext<AiGetCurrentLearningGoal> context)
    {
        var req = context.Message;
        var student = await studentRepository.FirstOrDefaultAsync(x => x.StudentId == req.UserId && x.IsActive);

        var current = student?.LearningGoals?
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        var dto = current?.Goal == null
            ? null
            : new AiCurrentLearningGoalDto
            {
                LearningGoalId = current.GoalId,
                LearningGoalName = current.Goal.GoalName,
                LearningGoalType = current.Goal.LearningGoalType
            };

        await context.RespondAsync(new AiGetCurrentLearningGoalResponse
        {
            Success = true,
            Response = dto
        });
    }
}


