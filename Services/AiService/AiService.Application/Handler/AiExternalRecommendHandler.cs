using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler
{
    public class AiExternalRecommendHandler : IRequestHandler<AiExternalCourseRequest, AiExternalCourseResponse>
    {
        private readonly IAdvisorService _advisorService;

        public AiExternalRecommendHandler(IAdvisorService advisorService)
        {
            _advisorService = advisorService;
        }
        public async Task<AiExternalCourseResponse> Handle(AiExternalCourseRequest request, CancellationToken cancellationToken)
        {
            var result = await _advisorService.AskAsync(request.GoalMajor, 80, true, cancellationToken);
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
