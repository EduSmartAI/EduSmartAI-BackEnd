using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningGoals.Commands;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

/// <summary>
/// InsertLearningGoalController - Insert new Learning Goal
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class InsertLearningGoalController : ControllerBase, IApiAsyncController<LearningGoalInsertCommand, LearningGoalInsertResponse>
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
    public InsertLearningGoalController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
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
    public async Task<LearningGoalInsertResponse> ProcessRequest(LearningGoalInsertCommand request)
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