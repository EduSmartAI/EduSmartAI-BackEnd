using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using MassTransit;

namespace AiService.Application.Consumers.StudentInterestSurveyAnalysis;

public class StudentInterestSurveyAnalysisConsumer(ISurveyAnalysis surveyAnalysis) : IConsumer<StudentInterestSurveyAnalysisEvent>
{
    public async Task Consume(ConsumeContext<StudentInterestSurveyAnalysisEvent> context)
    {
        var request = context.Message;
        
        var result = await surveyAnalysis.AnalyzeStudentInterestSurveyAsync(request, context.CancellationToken);
        
        await context.RespondAsync(result);
    }
}
