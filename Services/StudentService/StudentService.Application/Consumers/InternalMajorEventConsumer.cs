using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers;

public class InternalMajorEventConsumer(
    ILearningPathService learningPathService,
    IPublishEndpoint publishEndpoint) : IConsumer<InternalMajorEvent>
{
    public async Task Consume(ConsumeContext<InternalMajorEvent> context)
    {
        var evt = context.Message;
        
        // Extract numeric value from LimitTime string (e.g., "6 months" -> 6)
        var limitTimeNumber = int.Parse(System.Text.RegularExpressions.Regex.Match(evt.LimitTime, @"\d+").Value);
        
        // Map event data to command
        var request = new LearningPathMajorInsertCommand
        {
            LearningPathId = evt.LearningPathId,
            MajorType = (short) ConstantEnum.LearningPathMajor.Internal,
            Majors = evt.Majors.Select(x => new LearningPathMajorRequest
            {
                MajorCode = x.MajorCode,
                Reason = x.Reason,
            }).ToList(),
            LimitTime = limitTimeNumber,
            StudentLevel = evt.StudentLevel,
            CurrentUserEmail = evt.CurrentUserEmail,
            SemesterId = evt.SemesterId,
            StudentPassedSubjects = evt.StudentPassedSubjects,
            CourseImproves = evt.CourseImproves?.Select(ci => new Applications.LearningPaths.Commands.CourseImprove
            {
                SubjectCode = ci.SubjectCode,
                SubjectPrerequisiteCode = ci.SubjectPrerequisiteCode,
                Level = ci.Level
            }).ToList(),
            StudentMajor = evt.StudentMajor,
            StudentTranscripts = evt.StudentTranscriptSelectEvent
        };

        if (!request.Majors.Select(x => x.MajorCode).Contains(request.StudentMajor.MajorCode))
        {
            request.Majors.Add(new LearningPathMajorRequest
            {
                MajorCode = request.StudentMajor.MajorCode,
                Reason = "Đây là những đánh giá, những môn học liên quan đến chuyên ngành hiện tại của bạn."
            });
        }

        // Insert internal majors using the learning path service
        var learningPathMajorInternalInsertResponse = await learningPathService.InsertLearningPathMajorAsync(request);
        
        if (learningPathMajorInternalInsertResponse.Success && (evt.SubjectMarks != null || evt.QuizSurvey != null) && learningPathMajorInternalInsertResponse.InsertedMajorIds != null)
        {
            try
            {
                // Map inserted LearningPathMajorIds to MajorCodes
                var majorInfos = evt.Majors
                    .Zip(learningPathMajorInternalInsertResponse.InsertedMajorIds, 
                        (major, majorId) => new MajorInfoEvent
                        {
                            MajorCode = major.MajorCode,
                            MajorName = major.MajorCode,
                            LearningPathMajorId = majorId
                        })
                    .ToList();
                
                // Publish AiRecommendImprovementEvent with ALL majors (with their IDs)
                var aiEvent = new AiRecommendImprovementEvent
                {
                    CareerGoal = evt.CareerGoal ?? string.Empty,
                    Majors = majorInfos,
                    SubjectMarks = evt.SubjectMarks?.Select(sm => new SubjectMarkEvent
                    {
                        SubjectCode = sm.SubjectCode,
                        SubjectName = sm.SubjectName,
                        Mark = sm.Mark
                    }).ToList() ?? new List<SubjectMarkEvent>(),
                    AbilityMarks = evt.AbilityMarks?.Select(am => new AbilityMarkEvent
                    {
                        Name = am.Name,
                        Mark = am.Mark
                    }).ToList(),
                    QuizSurveyEvent = evt.QuizSurvey != null ? new QuizSurveyEvent
                    {
                        QuizInterests = evt.QuizSurvey.QuizInterests.Select(qi => new QuizInterestEvent
                        {
                            Question = qi.Question,
                            Answer = qi.Answer
                        }).ToList(),
                        QuizHabits = evt.QuizSurvey.QuizHabits.Select(qh => new QuizHabitEvent
                        {
                            Question = qh.Question,
                            Answer = qh.Answer
                        }).ToList()
                    } : new QuizSurveyEvent(),
                    LearningPathMajorId = learningPathMajorInternalInsertResponse.StudentMajorId,
                    LearningPathId = evt.LearningPathId,
                    Email = evt.StudentEmail ?? evt.CurrentUserEmail,
                    StudentCurriculums = learningPathMajorInternalInsertResponse.StudentCurriculums
                };
                
                await publishEndpoint.Publish(aiEvent, context.CancellationToken);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the response
                // AI feedback generation is async and can be retried
                Console.WriteLine($"Failed to publish AiRecommendImprovementEvent: {ex.Message}");
            }
        }
        
        await context.RespondAsync(new InternalMajorEventResponse
        {
            Success = learningPathMajorInternalInsertResponse.Success,
            Message = learningPathMajorInternalInsertResponse.Message,
            MessageId = learningPathMajorInternalInsertResponse.MessageId
        });
    }
}