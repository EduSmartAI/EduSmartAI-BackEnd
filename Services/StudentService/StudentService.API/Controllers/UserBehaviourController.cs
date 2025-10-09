using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.UserBehaviours.Commands;
using Swashbuckle.AspNetCore.Annotations;


namespace StudentService.API.Controllers;

/// <summary>
/// UserBehaviourController - Manages user behaviour tracking
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class UserBehaviourController : ControllerBase
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
    public UserBehaviourController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Insert user behaviour tracking
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Dùng cho việc lưu hành vi của người dùng để theo dõi đưa ra lộ trình cá nhân hoá phù hợp",
        Description = "Cần cấp quyền"
    )]    public async Task<UserBehaviourInsertResponse> InsertUserBehaviour([FromBody] UserBehaviourInsertCommand request)
    {
        return await ApiControllerHelper
            .HandleRequest<UserBehaviourInsertCommand, UserBehaviourInsertResponse, string>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new UserBehaviourInsertResponse());
    }
}