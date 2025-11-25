using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.LearningPaths;

public class InsertLearningPathWithPreviousSurveyAndTranscriptHandler(ILearningPathService learningPathService) : ICommandHandler<InsertLearningPathWithPreviousSurveyAndTranscriptCommand, InsertLearningPathWithPreviousSurveyAndTranscriptResponse>
{
    public async Task<InsertLearningPathWithPreviousSurveyAndTranscriptResponse> Handle(InsertLearningPathWithPreviousSurveyAndTranscriptCommand request, CancellationToken cancellationToken)
    {
        return await learningPathService.InsertLearningPathWithPreviousSurveyAndTranscriptAsync(request, cancellationToken);
    }
}