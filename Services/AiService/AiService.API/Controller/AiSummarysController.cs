using AiService.Application.Features.AiSummary;
using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;

namespace AiService.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiSummarysController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIdentityService _identityService;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IHttpContextAccessor _httpContextAccessor;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="identityService"></param>
        /// <param name="httpContextAccessor"></param>
        public AiSummarysController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _identityService = identityService;
            _httpContextAccessor = httpContextAccessor;
        }
        /// <summary>
        /// Summary feedback course overall
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("feedback-course")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<AiSummaryResponse> GenSummaryOverviewCourse(AiSummaryRequest request)
        {
            var placeholder = new IdentityEntity();
            return await ApiControllerHelper.HandleRequest<AiSummaryRequest, AiSummaryResponse, string>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                placeholder,
                _httpContextAccessor,
                new AiSummaryResponse());
        }
    }
}