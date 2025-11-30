using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.AIService;
using MassTransit;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Consumers;

public class LearningFeedbackEventConsumer : IConsumer<LearningFeedbackEvent>
{
    private readonly ICommandRepository<LearningPath> _learningPathRepository;
    private readonly ICommandRepository<LearningPathSubjectCode> _learningPathSubjectCodeRepository;
    private readonly IQueryRepository<LearningPathCollection> _learningPathQueryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LearningFeedbackEventConsumer(ICommandRepository<LearningPath> learningPathRepository, ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeRepository, IQueryRepository<LearningPathCollection> learningPathQueryRepository, IUnitOfWork unitOfWork)
    {
        _learningPathRepository = learningPathRepository;
        _learningPathSubjectCodeRepository = learningPathSubjectCodeRepository;
        _learningPathQueryRepository = learningPathQueryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
    {
        var evt = context.Message;
        
        var learningPath = await _learningPathRepository.FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive)!;
        
        // Update learning path
        learningPath!.SummaryFeedback = evt.SummaryFeedback;
        learningPath.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
        learningPath.Personality = evt.Personality;
        learningPath.LearningAbility = evt.LearningAbility;
        _learningPathRepository.Update(learningPath);
        
        // Insert LearningPathSubjectCodes
        foreach (var subCode in evt.LearningPathSubjectCodes)
        {
            var learningPathSubjectCode = new LearningPathSubjectCode
            {
                LearningPathId = learningPath.PathId,
                SubjectCode = subCode.SubjectCode,
                AnalysisMarkdown = subCode.AnalysisMarkdown,
            };
            await _learningPathSubjectCodeRepository.AddAsync(learningPathSubjectCode);
        }
        
        await _unitOfWork.SaveChangesAsync();
        
        // Todo: Update in Read Model
        // Store to Collection
        // var learningPathCollection = await _learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive);
        //
        // learningPathCollection!.SummaryFeedback = evt.SummaryFeedback;
        // learningPathCollection.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
        // learningPathCollection.Personality = evt.Personality;
        // learningPathCollection.LearningAbility = evt.LearningAbility;
    }
}