using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler
{
    public class AiRecommendHandler : IRequestHandler<AiEvaluateRequest, AiEvaluateResponse>
    {
        private readonly IAdvisorService _advisorService;

        public AiRecommendHandler(IAdvisorService advisorService)
        {
            _advisorService = advisorService;
        }
        public async Task<AiEvaluateResponse> Handle(AiEvaluateRequest request, CancellationToken cancellationToken)
        {
            var result = await _advisorService.EvaluateAsync(request, cancellationToken);
            if (result == null)
            {
                throw new Exception("Error");
            }
            return new AiEvaluateResponse
            {
                Success = true,
                Message = "Uploaded successfully",
                Response = result
            };
        }
    }
}
