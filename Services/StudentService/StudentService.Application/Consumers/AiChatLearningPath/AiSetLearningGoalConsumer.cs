using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Consumers.AiChatLearningPath;

public class AiSetLearningGoalConsumer(
    IUnitOfWork unitOfWork,
    ICommandRepository<StudentLearningGoal> studentLearningGoalRepository,
    IQueryRepository<StudentCollection> studentCollectionRepository,
    IQueryRepository<LearningGoalCollection> learningGoalRepository
) : IConsumer<AiSetLearningGoal>
{
    public async Task Consume(ConsumeContext<AiSetLearningGoal> context)
    {
        var req = context.Message;

        if (req.UserId == Guid.Empty || req.LearningGoalId == Guid.Empty || string.IsNullOrWhiteSpace(req.Email))
        {
            await context.RespondAsync(new AiSetLearningGoalResponse
            {
                Success = false,
                Message = "Thiếu thông tin người dùng hoặc mục tiêu học tập."
            });
            return;
        }

        var goal = await learningGoalRepository.FirstOrDefaultAsync(x => x.GoalId == req.LearningGoalId && x.IsActive);
        if (goal == null)
        {
            await context.RespondAsync(new AiSetLearningGoalResponse
            {
                Success = false,
                Message = "Không tìm thấy mục tiêu học tập phù hợp."
            });
            return;
        }

        StudentLearningGoal? newLink = null;

        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // Deactivate existing goals (logical delete)
            var existing = await studentLearningGoalRepository
                .Find(x => x.StudentId == req.UserId && x.IsActive, cancellationToken: context.CancellationToken)
                .ToListAsync(context.CancellationToken);

            foreach (var item in existing)
            {
                studentLearningGoalRepository.Update(item);
            }

            if (existing.Count > 0)
            {
                await unitOfWork.SaveChangesAsync(req.Email, context.CancellationToken, needLogicalDelete: true);
            }

            // Insert new goal link
            newLink = new StudentLearningGoal
            {
                StudentId = req.UserId,
                GoalId = req.LearningGoalId
            };

            await studentLearningGoalRepository.AddAsync(newLink, req.Email);
            await unitOfWork.SaveChangesAsync(req.Email, context.CancellationToken);

            return true;
        }, context.CancellationToken);

        // Update StudentCollection read model (for later regeneration to pick up the latest goal)
        var student = await studentCollectionRepository.FirstOrDefaultAsync(x => x.StudentId == req.UserId && x.IsActive);
        if (student != null && newLink != null)
        {
            student.LearningGoals ??= new List<StudentLearningGoalCollection>();

            // mark old active goals inactive in read model (best-effort)
            foreach (var g in student.LearningGoals.Where(x => x.IsActive))
            {
                g.IsActive = false;
                g.UpdatedAt = DateTime.UtcNow;
                g.UpdatedBy = req.Email;
            }

            var newCollection = StudentLearningGoalCollection.FromWriteModel(newLink, goal);
            student.LearningGoals.Add(newCollection);
            student.UpdatedAt = DateTime.UtcNow;
            student.UpdatedBy = req.Email;

            unitOfWork.Store(student);
            await unitOfWork.SessionSaveChangesAsync();
        }

        await context.RespondAsync(new AiSetLearningGoalResponse
        {
            Success = true,
            Response = true,
            Message = "Đã cập nhật mục tiêu học tập."
        });
    }
}


