using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;
using MassTransit.Initializers;
using MediatR;

namespace AiService.Application.Consumers.StudentMajorRecommends;

public class StudentMajorRecommendHandler(IAdvisorService advisorService) : IRequestHandler<StudentMajorRecommendRequest, StudentMajorOrientationEventResponse>
{
    public async Task<StudentMajorOrientationEventResponse> Handle(StudentMajorRecommendRequest request, CancellationToken cancellationToken)
    {
        var response = new StudentMajorOrientationEventResponse {Success = false};
        
        // Set up the evaluate request
        var evaluateRequest = new AiEvaluateRequest
        {
            CareerGoal = request.LearningGoal,
            KnownFrameworks = request.Frameworks,
            KnownLanguages = request.Languages
        };
        
        try
        {
            // Call the advisor service to evaluate
            var evaluateResult = await advisorService.EvaluateAsync(evaluateRequest, cancellationToken);

            // Process the evaluate result
            var majorInternals = evaluateResult.Evaluations.Select(x => new MajorInternal
            {
                MajorName = x.MajorName,
                Reason = x.Reasons
            }).ToList();
            
            // Process the evaluate result
            var majorExternals = evaluateResult.Evaluations.Select(x => new MajorExternal
            {
                MajorName = x.MajorName,
                Reason = x.Reasons
            }).ToList();
            
            // Set the response data
            response.Response = new StudentMajorOrientationEventResponseEntity
            {
                MajorInternals = majorInternals,
                MajorExternals = majorExternals
            };

        }
        catch (Exception e)
        {
           response.Success = false;
           response.SetMessage(MessageId.E99999);
           return response;
        }
        
        // Set response
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Đề xuất định hướng nghề nghiệp cho sinh viên");
        return response;
    }
}