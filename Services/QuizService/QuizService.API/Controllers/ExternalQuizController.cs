using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;
using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;
using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.ExternalApplications;
using Swashbuckle.AspNetCore.Annotations;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.API.Controllers;

/// <summary>
/// ExternalQuizController - Provide external APIs for other services
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ExternalQuizController : ControllerBase
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
    public ExternalQuizController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Select all semesters
    /// </summary>
    /// <param name="questionId"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách tất cả các học kỳ",
        Description = "Cần cấp quyền cho API"
    )]
    public async Task<SemesterSelectsEventResponse> SelectSemesters()
    {
        var request = new ExternalSemesterSelectsQuery();
        
        return await ApiControllerHelper.HandleRequest<ExternalSemesterSelectsQuery, SemesterSelectsEventResponse, List<SemesterSelectsEventResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SemesterSelectsEventResponse());
    }
    
    /// <summary>
    /// Select all majors
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách tất cả các chuyên ngành trong trường",
        Description = "Cần cấp quyền cho API"
    )]
    public async Task<MajorSelectsEventResponse> SelectMajors()
    {
        var request = new ExternalMajorSelectsQuery();
        
        return await ApiControllerHelper.HandleRequest<ExternalMajorSelectsQuery, MajorSelectsEventResponse, List<MajorSelectsEventResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new MajorSelectsEventResponse());
    }
    
    /// <summary>
    /// Select all technologies
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách tất cả các ngôn ngữ lập trình/ framework",
        Description = "Cần cấp quyền cho API"
    )]
    public async Task<TechnologySelectsEventResponse> SelectTechnologies()
    {
        var request = new ExternalTechnologySelectsQuery();
        
        return await ApiControllerHelper.HandleRequest<ExternalTechnologySelectsQuery, TechnologySelectsEventResponse, List<TechnologySelectsEventResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new TechnologySelectsEventResponse());
    }
    
    /// <summary>
    /// Select all learning goals
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách tất cả các mục tiêu học tập đang có",
        Description = "Cần cấp quyền cho API"
    )]
    public async Task<LearningGoalSelectsEventResponse> SelectLearningGoals()
    {
        var request = new ExternalLearningGoalSelectsQuery();
        
        return await ApiControllerHelper.HandleRequest<ExternalLearningGoalSelectsQuery, LearningGoalSelectsEventResponse, List<LearningGoalSelectsEventResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningGoalSelectsEventResponse());
    }
}