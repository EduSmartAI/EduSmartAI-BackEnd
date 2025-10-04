using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;

namespace AiService.Application.Interfaces;

public interface ISurveyAnalysis
{
    Task<StudentInterestSurveyAnalysisEventResponse> AnalyzeStudentInterestSurveyAsync(StudentInterestSurveyAnalysisEvent request, CancellationToken cancellationToken = default);
}