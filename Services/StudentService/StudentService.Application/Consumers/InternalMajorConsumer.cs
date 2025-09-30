using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers;

public class InternalMajorConsumer(ILearningPathService learningPathService) : IConsumer<InternalMajorEvent>
{
    public async Task Consume(ConsumeContext<InternalMajorEvent> context)
    {
        var evt = context.Message;
        
        // Extract numeric value from LimitTime string (e.g., "6 months" -> 6)
        var limitTimeNumber = int.Parse(System.Text.RegularExpressions.Regex.Match(evt.LimitTime, @"\d+").Value);
        
        // Map event data to command
        var request = new LearningPathMajorInsertCommand
        {
            LearningPathId = evt.LearningPathId,
            MajorType = (short) ConstantEnum.LearningPathMajor.Internal,
            Majors = evt.Majors.Select(x => new LearningPathMajorRequest
            {
                MajorCode = x.MajorCode,
                Reason = x.Reason,
            }).ToList(),
            LimitTime = limitTimeNumber,
            CurrentUserEmail = evt.CurrentUserEmail
        };

        // Insert internal majors using the learning path service
        await learningPathService.InsertLearningPathMajorAsync(request);
        
        
    }
}