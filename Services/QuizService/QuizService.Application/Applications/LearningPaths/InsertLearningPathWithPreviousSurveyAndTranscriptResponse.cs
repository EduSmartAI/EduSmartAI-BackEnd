using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.LearningPaths;

public record InsertLearningPathWithPreviousSurveyAndTranscriptResponse : AbstractApiResponse<InsertLearningPathWithPreviousSurveyAndTranscriptResponseEntity>
{
    public override InsertLearningPathWithPreviousSurveyAndTranscriptResponseEntity Response { get; set; }
}

public class InsertLearningPathWithPreviousSurveyAndTranscriptResponseEntity
{
    public Guid LearningPathId { get; set; }
}