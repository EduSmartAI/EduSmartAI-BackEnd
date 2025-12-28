using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.PaymentService;
using Course.Application.UserLessonProgresses.Commands.EnrollCourse;
using Course.Domain.Models;
using Course.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Course.Application.Consumers;

public class PaymentSucceededEventConsumer(
    ICommandRepository<CourseStudentEnrollment> repository, 
    IUnitOfWork unitOfWork,
    ILogger<PaymentSucceededEventConsumer> logger) : IConsumer<PaymentSucceededEvent>
{
    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var evt = context.Message;
        
        try
        {
            foreach (var courseId in evt.CourseIds)
            {
                var enrollment = new CourseStudentEnrollment
                {
                    EnrollmentId = Guid.NewGuid(),
                    CourseId = courseId,
                    UserId = evt.UserId,
                    StartedAt = DateTime.UtcNow,
                    ExpiresAt = null,
                };

                await repository.AddAsync(enrollment, evt.Email);
                unitOfWork.Store(CourseStudentEnrollmentCollection.FromWriteModel(enrollment));
            }
            
            var savedCount = await unitOfWork.SaveChangesAsync(CancellationToken.None);
            await unitOfWork.SessionSaveChangesAsync();
            if (savedCount == 0)
            { 
                var failResponse = new PaymentSucceededEventResponse
                {
                    Response = "Failed to save enrollments to database.",
                    Success = false
                };
                await context.RespondAsync(failResponse);
                return;
            }

            var successResponse = new PaymentSucceededEventResponse
            {
                Response = "Success",
                Success = true
            };
            await context.RespondAsync(successResponse);
            
            logger.LogInformation("Successfully processed PaymentSucceededEvent for UserId: {UserId}", evt.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PaymentSucceededEvent for UserId: {UserId}", evt.UserId);
            
            var errorResponse = new PaymentSucceededEventResponse
            {
                Response = $"Error enrolling user in courses: {ex.Message}",
                Success = false
            };
            await context.RespondAsync(errorResponse);
            throw;
        }
    }
}