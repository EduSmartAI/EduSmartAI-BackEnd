using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.Messaging.Events.QuizService;
using QuizService.Domain.ReadModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Application.Applications.LearningPaths;

public class LearningPathCreationContext
{
    public List<StudentQuizCollection> StudentQuizCollections { get; init; } = null!;
    public IdentityEntity CurrentUser { get; init; } = null!;
    public StudentInformationSelectsEventResponseEntity InformationResponse { get; init; } = null!;
    public Guid LearningPathId { get; init; }
    public int LimitTime { get; init; }
    public short StudentLevel { get; init; }
    
    public List<string>? StudentPassedSubjects { get; init; }
}

