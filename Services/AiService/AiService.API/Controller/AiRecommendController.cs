using AiService.Application.Features.AiEvaluate;
using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Features.AiRecommend;
using AiService.Application.Features.AiSearch;
using AiService.Application.Features.AiSubjectCourse;
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
    private readonly IdentityEntity _identityEntity = default!;
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
    public async Task<AiEvaluateResponse> GetLearningPathAI(AiEvaluateTempRequest request)
    {
        return await ApiControllerHelper.HandleRequest<AiEvaluateTempRequest, AiEvaluateResponse, EvaluateResult>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AiEvaluateResponse());
    }
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<AiEvaluateResponse> GetLearningPathAiV2(AiEvaluationV2Request request)
    {
        return await ApiControllerHelper.HandleRequest<AiEvaluationV2Request, AiEvaluateResponse, EvaluateResult>(
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
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<AiRecommendImprovementResponse> GenAnalysis(AiRecommendImprovementRequest request)
    {
        return await ApiControllerHelper.HandleRequest<AiRecommendImprovementRequest, AiRecommendImprovementResponse, AiAnalysisSubjectAndAbilityDto>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AiRecommendImprovementResponse());
    }

    [HttpPost("subject-course-match")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<AiSubjectCourseResponse> MatchSubjectCourses(AiSubjectCourseRequest request)
    {
        return await ApiControllerHelper.HandleRequest<AiSubjectCourseRequest, AiSubjectCourseResponse, SubjectCourseMatchResult>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AiSubjectCourseResponse());
    }

    /// <summary>
    /// Analyze subject mark and provide improvement suggestions with dependent subject warnings
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("subject-analysis")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<SubjectAnalysisResponse> AnalyzeSubject(SubjectAnalysisRequest request)
    {
        return await ApiControllerHelper.HandleRequest<SubjectAnalysisRequest, SubjectAnalysisResponse, SubjectAnalysisDto>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SubjectAnalysisResponse());
    }

    /// <summary>
    /// Analyze subject by courseId - automatically gets subject info and score
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("course-subject-analysis")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<SubjectAnalysisResponse> AnalyzeCourseSubject(CourseSubjectAnalysisRequest request)
    {
        return await ApiControllerHelper.HandleRequest<CourseSubjectAnalysisRequest, SubjectAnalysisResponse, SubjectAnalysisDto>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SubjectAnalysisResponse());
    }

    /// <summary>
    /// Analyze subject mark update - compares old mark and new mark with new analysis
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("subject-mark-update")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<SubjectMarkUpdateResponse> AnalyzeSubjectMarkUpdate(SubjectMarkUpdateRequest request)
    {
        return await ApiControllerHelper.HandleRequest<SubjectMarkUpdateRequest, SubjectMarkUpdateResponse, SubjectMarkUpdateDto>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SubjectMarkUpdateResponse());
    }
}