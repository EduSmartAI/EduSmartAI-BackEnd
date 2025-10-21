using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
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
    public class AIChatBotsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIdentityService _identityService;
        private readonly IdentityEntity _identityEntity;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IHttpContextAccessor _httpContextAccessor;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="identityService"></param>
        /// <param name="httpContextAccessor"></param>
        public AIChatBotsController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _identityService = identityService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Chat with AI
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<AIChatBotResponse> ChatWithAIAsync(AIChatBotRequest request)
        {
            return await ApiControllerHelper.HandleRequest<AIChatBotRequest, AIChatBotResponse, ChatResponseDto>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new AIChatBotResponse());
        }
    }
}
