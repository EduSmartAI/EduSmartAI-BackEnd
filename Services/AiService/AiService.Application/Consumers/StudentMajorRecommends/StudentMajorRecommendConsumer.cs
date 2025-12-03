using AiService.Application.Features.AiEvaluate;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using MediatR;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace AiService.Application.Consumers.StudentMajorRecommends;

public class StudentMajorRecommendConsumer(IMediator mediator) : IConsumer<StudentMajorOrientationEvent>
{
    public async Task Consume(ConsumeContext<StudentMajorOrientationEvent> context)
    {
        var evt = context.Message;

        var request = new AiEvaluateRequest
        {
            CareerGoal = evt.LearningGoal,
            KnownFrameworks = evt.Frameworks,
            KnownLanguages = evt.Languages,
            IdentityEntity = new IdentityEntity
            {
                UserId = evt.IdentityEntity.UserId,
                Email = evt.IdentityEntity.Email
            },
            ExternalLimitTime = evt.LimitTime,
            LearningPathId = evt.LearningPathId,
            SemesterId = evt.SemesterId,
            StudentLevel = evt.StudentLevel,
            StudentPassedSubjects = evt.StudentPassedSubjects ?? null,
            CourseImproves = evt.CourseImproves ?? null,
            SubjectMarks = evt.SubjectMarks?.Select(sm => new StudentSubjectMarkRequest
            {
                SubjectCode = sm.SubjectCode,
                SubjectName = sm.SubjectName,
                Mark = sm.Mark
            }).ToList(),
            
            AbilityMarks = evt.AbilityMarks?.Select(am => new StudentAbilityMarkRequest
            {
                Name = am.Name,
                Mark = am.Mark
            }).ToList(),
            
            QuizSurvey = evt.QuizSurvey != null ? new StudentQuizSurveyRequest
            {
                QuizInterests = evt.QuizSurvey.QuizInterests.Select(qi => new StudentQuizInterestRequest
                {
                    Question = qi.Question,
                    Answer = qi.Answer
                }).ToList(),
                QuizHabits = evt.QuizSurvey.QuizHabits.Select(qh => new StudentQuizHabitRequest
                {
                    Question = qh.Question,
                    Answer = qh.Answer
                }).ToList()
            } : null,
            StudentMajor = evt.StudentMajor,
            StudentTranscrpts = evt.StudentTranscripts
        };
        
        await mediator.Send(request, context.CancellationToken);
    }
}