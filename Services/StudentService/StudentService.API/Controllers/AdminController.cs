using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.Technologies.Commands;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;
    private readonly IdentityEntity _identityEntity;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminController(IIdentityService identityService, IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _identityService = identityService;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin,
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Thêm ngôn ngữ/ framework mới",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<TechnologyInsertResponse> InsertTechnology([FromBody] TechnologyInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TechnologyInsertCommand, TechnologyInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new TechnologyInsertResponse()
        );
    }
    
    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Tạo mục tiêu học tập mới",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<LearningGoalInsertResponse> InsertLearningGoal(LearningGoalInsertCommand request)
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
}