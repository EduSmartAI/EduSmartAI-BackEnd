using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;

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
                MajorId = m.LearningPathMajorId,
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
        
        // Create a dictionary for quick lookup of curriculum status by SubjectCode
        var curriculumStatusMap = evt.StudentCurriculums?
            .ToDictionary(c => c.SubjectCode, c => c.Status.GetDescription(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
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
                AnalysisMarkdown = x.AnalysisMarkdown,
                Status = curriculumStatusMap.TryGetValue(x.SubjectCode, out var status) ? status : "Not Started"
            }).ToList(),
            LearningPathMajorId = evt.LearningPathMajorId,
            Email = evt.Email,
            LearningPathId = evt.LearningPathId,
            Majors = evt.Majors,
            AbilityAnalyses = generateLearningFeedbackResult.Response.AbilityAnalyses.Select(x => new AbilityAnalysisEvent
            {
                Name = x.Name,
                AnalysisMarkdown = x.AnalysisMarkdown
            }).ToList()
        };
        
        learningFeedbackEvent.LearningPathSubjectCodes.AddRange(generateLearningFeedbackResult.Response.WithoutMarkAnalysis.Select(x => new LearningPathSubjectCodeEvent
        {
            SubjectCode = x.SubjectCode,
            AnalysisMarkdown = x.AnalysisMarkdown,
            Status = curriculumStatusMap.TryGetValue(x.SubjectCode, out var status) ? status : "Not Started"
        }));
        
        // Add remaining subjects from request.StudentCurriculums that are not in SubjectAnalyses or WithoutMarkAnalysis
        if (evt.StudentCurriculums != null && evt.StudentCurriculums.Any())
        {
            var existingSubjectCodes = learningFeedbackEvent.LearningPathSubjectCodes
                .Select(x => x.SubjectCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            // Create a dictionary for quick lookup of subject marks
            var subjectMarkMap = evt.SubjectMarks
                .Where(sm => sm.Mark.HasValue)
                .ToDictionary(sm => sm.SubjectCode, sm => sm.Mark!.Value, StringComparer.OrdinalIgnoreCase);
            
            var missingSubjects = evt.StudentCurriculums
                .Where(c => !existingSubjectCodes.Contains(c.SubjectCode))
                .Select(c => new LearningPathSubjectCodeEvent
                {
                        SubjectCode = c.SubjectCode,
                        AnalysisMarkdown = null,
                        Status = c.Status.GetDescription(),
                })
                .ToList();
            
            learningFeedbackEvent.LearningPathSubjectCodes.AddRange(missingSubjects);
        }
        
        await context.Publish(learningFeedbackEvent, context.CancellationToken);
    }
}