using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.Majors.Commands;
using StudentService.Application.Applications.Majors.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MajorController : ControllerBase
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
    public MajorController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
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
        Summary = "Tạo chuyên ngành mới",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<IActionResult> InsertMajor(MajorInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<MajorInsertCommand, MajorInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new MajorInsertResponse()
        );
    }
    
    /// <summary>
    /// Incoming Get
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách chuyên ngành",
        Description = "Trả về danh sách tất cả chuyên ngành đang hoạt động. Ai cũng có thể xem"
    )]
    public async Task<IActionResult> SelectMajors([FromQuery] MajorsSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<MajorsSelectQuery, MajorsSelectResponse, List<MajorsSelectResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            new MajorsSelectResponse()
        );
    }
}