using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;

namespace AiService.Application.Consumers;

public class AiRecommendImprovementEventConsumer(IAiSummaryService aiSummaryService) : IConsumer<AiRecommendImprovementEvent>
{
    public async Task Consume(ConsumeContext<AiRecommendImprovementEvent> context)
    {
        var evt = context.Message;
        var request = new AiRecommendImprovementRequest
        {
            CareerGoal = evt.CareerGoal,
            Majors = evt.Majors.Select(m => new MajorInfo
            {
                MajorCode = m.MajorCode,
                MajorName = m.MajorName
            }).ToList(),
            
            QuizSurvey = new QuizSurvey
            {
                QuizHabits = evt.QuizSurveyEvent.QuizHabits.Select(x => new QuizHabit
                {
                    Question = x.Question,
                    Answer = x.Answer
                }).ToList(),
                QuizInterests = evt.QuizSurveyEvent.QuizInterests.Select(x => new QuizInterest
                {
                    Question = x.Question,
                    Answer = x.Answer
                }).ToList(),
            },
            AbilityMarks = evt.AbilityMarks?.Select(x => new AbilityMark
            {
                Mark = x.Mark,
                Name = x.Name
            }).ToList(),
            SubjectMarks = evt.SubjectMarks.Select(x => new SubjectMark
            {
                Mark = x.Mark,
                SubjectCode = x.SubjectCode,
                SubjectName = x.SubjectName
            }).ToList(),
        };
        
        var generateLearningFeedbackResult = await aiSummaryService.GenerateLearningFeedbackMarkdownAsync(request, context.CancellationToken);
        
        // Publish event to StudentService to insert learning feedback
        var learningFeedbackEvent = new LearningFeedbackEvent
        {
            SummaryFeedback = generateLearningFeedbackResult.Response.SummaryFeedback,
            HabitAndInterestAnalysis = generateLearningFeedbackResult.Response.HabitAndInterestAnalysis,
            LearningAbility = generateLearningFeedbackResult.Response.LearningAbility,
            Personality = generateLearningFeedbackResult.Response.Personality,
            LearningPathSubjectCodes = generateLearningFeedbackResult.Response.SubjectAnalyses.Select(x => new LearningPathSubjectCodeEvent
            {
                SubjectCode = x.SubjectCode,
                AnalysisMarkdown = x.AnalysisMarkdown
            }).ToList(),
            LearningPathId = evt.LearningPathId,
            Email = evt.Email
        };
        
        learningFeedbackEvent
            .LearningPathSubjectCodes
            .AddRange(generateLearningFeedbackResult.Response.WithoutMarkAnalysis.Select(x => new LearningPathSubjectCodeEvent
        {
            SubjectCode = x.SubjectCode,
            AnalysisMarkdown = x.AnalysisMarkdown
        }));
        await context.Publish(learningFeedbackEvent, context.CancellationToken);
    }
}