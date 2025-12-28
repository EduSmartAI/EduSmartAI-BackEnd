using AiService.Application.Features.AiSummary;
using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;

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

        /// <summary>
        /// Preview feedback course overall (ONLY generate AI content, DO NOT save to DB)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("feedback-course-preview")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<AiSummaryResponse> PreviewSummaryOverviewCourse(AiSummaryPreviewRequest request)
        {
            var placeholder = new IdentityEntity();
            return await ApiControllerHelper.HandleRequest<AiSummaryPreviewRequest, AiSummaryResponse, string>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                placeholder,
                _httpContextAccessor,
                new AiSummaryResponse());
        }
        /// <summary>
        /// Gen and summary feedback module
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("feedback-module")]
        [SwaggerOperation(
            Summary = "Generate AI summary feedback module",
            Description = "Generates an AI-powered summary feedback module based on the provided request data. Requires authentication."
        )]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<AiSummaryFeedbackModuleResponse> GenFeedbackModule(AiSummaryFeedbackModuleRequest request)
        {
            var placeholder = new IdentityEntity();
            return await ApiControllerHelper.HandleRequest<AiSummaryFeedbackModuleRequest, AiSummaryFeedbackModuleResponse, string>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                placeholder,
                _httpContextAccessor,
                new AiSummaryFeedbackModuleResponse());
        }
    }
}