using QuizService.Application.Applications.LearningPaths;

namespace QuizService.Application.Interfaces;

public interface ILearningPathService
{
    Task<InsertLearningPathWithPreviousSurveyAndTranscriptResponse> InsertLearningPathWithPreviousSurveyAndTranscriptAsync(InsertLearningPathWithPreviousSurveyAndTranscriptCommand request, CancellationToken cancellationToken);
   
    Task<LearningPathCreationResult> CreateLearningPathAsync(LearningPathCreationContext context, CancellationToken cancellationToken);
    
    Task<StudentLevelCalculationResult> CalculateStudentLevelFromTranscriptAsync(Guid studentId, CancellationToken cancellationToken);
}