using AiService.Application.Features.AiEvaluate;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using MediatR;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace AiService.Application.Consumers.StudentMajorRecommends;

public class StudentMajorRecommendConsumer(IMediator mediator) : IConsumer<StudentMajorOrientationEvent>
{
    public async Task Consume(ConsumeContext<StudentMajorOrientationEvent> context)
    {
        var evt = context.Message;

        var request = new AiEvaluateRequest
        {
            CareerGoal = evt.LearningGoal,
            KnownFrameworks = evt.Frameworks,
            KnownLanguages = evt.Languages,
            IdentityEntity = new IdentityEntity
            {
                UserId = evt.IdentityEntity.UserId,
                Email = evt.IdentityEntity.Email
            },
            ExternalLimitTime = evt.LimitTime,
            LearningPathId = evt.LearningPathId,
            SemesterId = evt.SemesterId,
        };
        
        await mediator.Send(request, context.CancellationToken);
    }
}