using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using MediatR;

namespace AiService.Application.Handler
{
    public class AiRecommendHandler : IRequestHandler<AiEvaluateRequest, AiEvaluateResponse>
    {
        private readonly IAdvisorService _advisorService;
        private readonly IPublishEndpoint _requestPublishEndpoint;
        private readonly IRequestClient<InsertLearningPathEvent> _requestClientInsertLearningPath;
        private readonly IRequestClient<InternalMajorEvent> _requestClientInternalMajor;
        private readonly IMediator _mediator;
        
        public AiRecommendHandler(IAdvisorService advisorService, IPublishEndpoint requestPublishEndpoint, IMediator mediator, IRequestClient<InsertLearningPathEvent> requestClientInsertLearningPath, IRequestClient<InternalMajorEvent> requestClientInternalMajor)
        {
            _advisorService = advisorService;
            _requestPublishEndpoint = requestPublishEndpoint;
            _mediator = mediator;
            _requestClientInsertLearningPath = requestClientInsertLearningPath;
            _requestClientInternalMajor = requestClientInternalMajor;
        }
        
        public async Task<AiEvaluateResponse> Handle(AiEvaluateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // AI recommend major
                var result = await _advisorService.EvaluateAsync(request, cancellationToken);
                var matched = result.Matched;
                var hasMatched = matched.Count > 0;

                Task<Response<InternalMajorEventResponse>>? internalInsertTask = null;
                Task<AiBatchExternalRecommendResponse>? externalInsertTask = null;

                if (hasMatched)
                {
                    // Publish message internal
                    var majors = matched
                        .Where(e => !string.IsNullOrWhiteSpace(e.MajorCode))
                        .Select(e => new InternalMajorItem(
                            MajorCode: e.MajorCode.Trim(),
                            Reason: string.IsNullOrWhiteSpace(e.Reasons) ? "—" : e.Reasons.Trim()
                        ))
                        .ToList();

                    var internalMajorEvent = new InternalMajorEvent(
                        LearningPathId: request.LearningPathId,
                        StudentLevel: request.StudentLevel,
                        LimitTime: request.ExternalLimitTime,
                        CurrentUserEmail: request.IdentityEntity.Email,
                        Majors: majors,
                        SemesterId: request.SemesterId,
                        StudentMajor: request.StudentMajor,
                        StudentPassedSubjects: request.StudentPassedSubjects,
                        CourseImproves: request.CourseImproves,
                        StudentTranscriptSelectEvent: request.StudentTranscrpts,
                        CareerGoal: request.CareerGoal,
                        SubjectMarks: request.SubjectMarks?.Select(sm => new SubjectMarkForAI(
                            SubjectCode: sm.SubjectCode,
                            SubjectName: sm.SubjectName,
                            Mark: sm.Mark
                        )).ToList(),
                        AbilityMarks: request.AbilityMarks?.Select(am => new AbilityMarkForAI(
                            Name: am.Name,
                            Mark: am.Mark
                        )).ToList(),
                        QuizSurvey: request.QuizSurvey != null ? new QuizSurveyForAI(
                            QuizInterests: request.QuizSurvey.QuizInterests.Select(qi => new QuizInterestForAI(
                                Question: qi.Question,
                                Answer: qi.Answer
                            )).ToList(),
                            QuizHabits: request.QuizSurvey.QuizHabits.Select(qh => new QuizHabitForAI(
                                Question: qh.Question,
                                Answer: qh.Answer
                            )).ToList()
                        ) : null,
                        StudentEmail: request.IdentityEntity.Email
                    );
                    internalInsertTask = _requestClientInternalMajor.GetResponse<InternalMajorEventResponse>(internalMajorEvent, cancellationToken);
                    
                }

                // Process external majors in batch instead of loop
                if ((result.ExternalSuggestions?.Count ?? 0) > 0)
                {
                    var externalMajors = result.ExternalSuggestions!
                        .Where(s => !string.IsNullOrWhiteSpace(s.MajorCode))
                        .Select(s => new ExternalMajorItem(
                            MajorCode: s.MajorCode!.Trim().ToUpperInvariant(),
                            Reason: string.IsNullOrWhiteSpace(s.WhyForYou) ? "—" : s.WhyForYou!.Trim(),
                            Description: string.IsNullOrWhiteSpace(s.Description) ? "—" : s.Description!.Trim(),
                            WhyForYou: string.IsNullOrWhiteSpace(s.WhyForYou) ? "—" : s.WhyForYou!.Trim()
                        ))
                        .GroupBy(x => x.MajorCode, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .Take(3)
                        .ToList();

                    if (externalMajors.Count > 0)
                    {
                        // Use batch processing instead of foreach loop
                        var batchRequest = new AiBatchExternalRecommendRequest
                        {
                            LearningPathId = request.LearningPathId.ToString(),
                            CurrentUserEmail = request.IdentityEntity.Email,
                            Majors = externalMajors.Select(m => new ExternalMajorRequestItem
                            {
                                MajorCode = m.MajorCode,
                                Reason = m.Reason
                            }).ToList()
                        };
                        
                        externalInsertTask = _mediator.Send(batchRequest, cancellationToken);
                    }
                }
                var tasksToWait = new List<Task>();
                if (internalInsertTask != null) 
                    tasksToWait.Add(internalInsertTask);
                if (externalInsertTask != null) 
                    tasksToWait.Add(externalInsertTask);

                if (tasksToWait.Any())
                {
                    await Task.WhenAll(tasksToWait);
                }

                var internalResult = (await internalInsertTask!).Message;
                var externalResult = await externalInsertTask!;

                if (internalResult.Success && externalResult.Success)
                {
                    var learningPathUpdateStatusEvent = new LearningPathUpdateStatusEvent
                    {
                        LearningPathId = request.LearningPathId,
                    };

                    await _requestPublishEndpoint.Publish(learningPathUpdateStatusEvent, cancellationToken);
                }
                
                
                return new AiEvaluateResponse
                {
                    Success = true,
                    Message = "Generate successfully",
                    Response = null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AiRecommendHandler: {ex.Message}");
                return new AiEvaluateResponse
                {
                    Success = false,
                    Message = "Generate failed: " + ex.Message
                };
            }
        }
    }
}
