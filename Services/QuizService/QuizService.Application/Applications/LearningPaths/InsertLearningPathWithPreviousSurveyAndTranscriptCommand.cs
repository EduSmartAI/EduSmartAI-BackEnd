using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.LearningPaths;

public class InsertLearningPathWithPreviousSurveyAndTranscriptCommand : ICommand<InsertLearningPathWithPreviousSurveyAndTranscriptResponse>
{
    public Guid LearningGoalId { get; set; }
    public string LearningGoalName { get; set; }
    public ConstantEnum.LearningGoalType LearningGoalType { get; set; }
}