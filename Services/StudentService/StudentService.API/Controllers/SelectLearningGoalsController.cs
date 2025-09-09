using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningGoals.Queris;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SelectLearningGoalsController : ControllerBase, IApiAsyncController<LearningGoalsSelectQuery, LearningGoalsSelectResponse>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;
    private readonly IdentityEntity _identityEntity;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="identityService"></param>
    /// <param name="httpContextAccessor"></param>
    public SelectLearningGoalsController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
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
    public async Task<LearningGoalsSelectResponse> ProcessRequest([FromQuery] LearningGoalsSelectQuery request)
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