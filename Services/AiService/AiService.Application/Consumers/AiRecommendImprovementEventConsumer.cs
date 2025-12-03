using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using BaseService.Common.Utils;

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
            StudentCurriculums = evt.StudentCurriculums
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
            LearningPathId = evt.LearningPathId
        };
        
        learningFeedbackEvent.LearningPathSubjectCodes.AddRange(generateLearningFeedbackResult.Response.WithoutMarkAnalysis.Select(x => new LearningPathSubjectCodeEvent
        {
            SubjectCode = x.SubjectCode,
            AnalysisMarkdown = x.AnalysisMarkdown,
            Status = curriculumStatusMap.TryGetValue(x.SubjectCode, out var status) ? status : "Not Started"
        }));
        
        // Add remaining subjects from request.StudentCurriculums that are not in SubjectAnalyses or WithoutMarkAnalysis
        if (request.StudentCurriculums != null && request.StudentCurriculums.Any())
        {
            var existingSubjectCodes = learningFeedbackEvent.LearningPathSubjectCodes
                .Select(x => x.SubjectCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            var missingSubjects = request.StudentCurriculums
                .Where(c => !existingSubjectCodes.Contains(c.SubjectCode))
                .Select(c => new LearningPathSubjectCodeEvent
                {
                    SubjectCode = c.SubjectCode,
                    AnalysisMarkdown = null,
                    Status = c.Status.GetDescription()
                })
                .ToList();
            
            learningFeedbackEvent.LearningPathSubjectCodes.AddRange(missingSubjects);
        }

        // Add mapping from subject code => ability name
        var subjectToAbilityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PRO192", "Lập trình hướng đối tượng" },
            { "CSD201", "Cấu trúc dữ liệu và giải thuật" },
            { "DBI202", "Cơ sở dữ liệu" },
            { "WED201c", "Lập trình web HTML/CSS cơ bản" }
        };

        var abilityAnalyses = generateLearningFeedbackResult.Response.AbilityAnalyses;

        foreach (var kv in subjectToAbilityMap)
        {
            var subjectCode = kv.Key;
            var abilityName = kv.Value;

            // Find existing subject entry (case-insensitive)
            var subjectEntry = learningFeedbackEvent.LearningPathSubjectCodes.FirstOrDefault(s => string.Equals(s.SubjectCode, subjectCode, StringComparison.OrdinalIgnoreCase));

            // Find corresponding ability analysis by name (case-insensitive)
            var abilityEntry = abilityAnalyses
                .FirstOrDefault(a => string.Equals(a.Name, abilityName, StringComparison.OrdinalIgnoreCase));

            if (abilityEntry == null || string.IsNullOrWhiteSpace(abilityEntry.AnalysisMarkdown))
                continue;

            if (subjectEntry != null)
            {
                subjectEntry.AnalysisMarkdown = string.IsNullOrWhiteSpace(subjectEntry.AnalysisMarkdown)
                    ? abilityEntry.AnalysisMarkdown
                    : subjectEntry.AnalysisMarkdown + Environment.NewLine + abilityEntry.AnalysisMarkdown;
            }
            else
            {
                // Create new subject entry if missing
                learningFeedbackEvent.LearningPathSubjectCodes.Add(new LearningPathSubjectCodeEvent
                {
                    SubjectCode = subjectCode,
                    AnalysisMarkdown = abilityEntry.AnalysisMarkdown,
                    Status = curriculumStatusMap.TryGetValue(subjectCode, out var status) ? status : "Not Started"
                });
            }
        }
        
        await context.Publish(learningFeedbackEvent, context.CancellationToken);
    }
}