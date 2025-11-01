using BaseService.API.BaseControllers;
using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queries;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Applications.Technologies.Queries;
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
    /// Update technology
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Admin,
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật ngôn ngữ/ framework",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<TechnologyUpdateResponse> UpdateTechnology([FromBody] TechnologyUpdateCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TechnologyUpdateCommand, TechnologyUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new TechnologyUpdateResponse()
        );
    }
    
    /// <summary>
    /// Delete technology
    /// </summary>
    /// <param name="technologyId"></param>
    /// <returns></returns>
    [HttpDelete("[action]")]
    [Authorize(Roles = ConstRole.Admin,
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Xóa ngôn ngữ/ framework",
        Description = "Cần cấp quyền Admin - Xóa logic"
    )]
    public async Task<TechnologyDeleteResponse> DeleteTechnology([FromQuery] Guid technologyId)
    {
        var command = new TechnologyDeleteCommand { TechnologyId = technologyId };
        return await ApiControllerHelper.HandleRequest<TechnologyDeleteCommand, TechnologyDeleteResponse, string>(
            command,
            _logger,
            ModelState,
            async () => await _mediator.Send(command),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new TechnologyDeleteResponse()
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
    public async Task<LearningGoalInsertResponse> InsertLearningGoal([FromBody] LearningGoalInsertCommand request)
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
    /// Update learning goal
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật mục tiêu học tập",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<LearningGoalUpdateResponse> UpdateLearningGoal([FromBody] LearningGoalUpdateCommand request)
    {
        return await ApiControllerHelper.HandleRequest<LearningGoalUpdateCommand, LearningGoalUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningGoalUpdateResponse()
        );
    }
    
    /// <summary>
    /// Delete learning goal
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Xóa mục tiêu học tập",
        Description = "Cần cấp quyền Admin - Xóa logic"
    )]
    public async Task<LearningGoalDeleteResponse> DeleteLearningGoal([FromQuery] LearningGoalDeleteCommand request)
    {
        return await ApiControllerHelper.HandleRequest<LearningGoalDeleteCommand, LearningGoalDeleteResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningGoalDeleteResponse()
        );
    }
    
    /// <summary>
    /// Select technologies
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách công nghệ (phân trang, tìm kiếm, lọc)",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<AdminTechnologiesSelectResponse> SelectTechnologies([FromQuery] AdminTechnologiesSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminTechnologiesSelectQuery, AdminTechnologiesSelectResponse, PagedResult<AdminTechnologyItem>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AdminTechnologiesSelectResponse()
        );
    }
    
    /// <summary>
    /// Select learning goals
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách mục tiêu học tập (phân trang, tìm kiếm, lọc)",
        Description = "Cần cấp quyền Admin"
    )]
    public async Task<AdminLearningGoalsSelectResponse> SelectLearningGoals([FromQuery] AdminLearningGoalsSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminLearningGoalsSelectQuery, AdminLearningGoalsSelectResponse, PagedResult<AdminLearningGoalItem>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AdminLearningGoalsSelectResponse()
        );
    }
    
}