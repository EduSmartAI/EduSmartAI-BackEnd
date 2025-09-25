using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiService.Application.Handler
{
    public class AiExternalRecommendHandler(IAdvisorService advisorService, ILogger<AiExternalRecommendHandler> _logger) : IRequestHandler<AiExternalCourseRequest, AiExternalCourseResponse>
    {
        public async Task<AiExternalCourseResponse> Handle(AiExternalCourseRequest request, CancellationToken cancellationToken)
        {
            var result = await advisorService.AskAsync(request.GoalMajor, 80, true, cancellationToken);
            _logger.LogInformation("Advisor result json: {Json}", JsonSerializer.Serialize(result));
            if (result == null)
            {
                throw new Exception("Error");
            }
            return new AiExternalCourseResponse
            {
                Success = true,
                Message = "Uploaded successfully",
                Response = result
            };
        }
    }
}
