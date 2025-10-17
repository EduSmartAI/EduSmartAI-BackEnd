using AiService.Application.Features.AiEvaluate;
using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Features.AiSearch;
using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.API.Controller;

[ApiController]
[Route("api/v1/[controller]")]
public class AiRecommendController : ControllerBase
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
    public AiRecommendController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Upload Video and publish to RabbitMQ for asyncronus
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<AiEvaluateResponse> GetLearningPathAI(AiEvaluateRequest request)
    {
        return await ApiControllerHelper.HandleRequest<AiEvaluateRequest, AiEvaluateResponse, EvaluateResult>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AiEvaluateResponse());
    }
    [HttpPost("external-courses")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<AiExternalCourseResponse> GenExternalCourseByAI(AiExternalCourseRequest request)
    {
        return await ApiControllerHelper.HandleRequest<AiExternalCourseRequest, AiExternalCourseResponse, AskResponse>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AiExternalCourseResponse());
    }
    /// <summary>
    /// Search improvement document
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("improvement-search-ai")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<AiSearchResponse> GenImprovement(AiSearchRequest request)
    {
        return await ApiControllerHelper.HandleRequest<AiSearchRequest, AiSearchResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AiSearchResponse());
    }
}