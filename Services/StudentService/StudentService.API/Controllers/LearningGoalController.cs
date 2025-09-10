using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queris;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

/// <summary>
/// LearningGoalController - Insert new Learning Goal
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class LearningGoalController : ControllerBase
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
    public LearningGoalController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Tạo mục tiêu học tập mới",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<IActionResult> InsertLearningGoal(LearningGoalInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<LearningGoalInsertCommand, LearningGoalInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningGoalInsertResponse()
        );
    }

    /// <summary>
    /// Incoming Get
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách mục tiêu học tập",
        Description = "Trả về danh sách tất cả mục tiêu học tập. Cần xác thực"
    )]
    public async Task<IActionResult> SelectLearningGoals([FromQuery] LearningGoalsSelectQuery request)
    {
        return await ApiControllerHelper
            .HandleRequest<LearningGoalsSelectQuery, LearningGoalsSelectResponse, List<LearningGoalsSelectResponseEntity>>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new LearningGoalsSelectResponse()
            );
    }
}