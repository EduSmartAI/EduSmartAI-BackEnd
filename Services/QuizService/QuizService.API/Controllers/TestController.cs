using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Applications.Tests.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// InsertTestController - Insert new Test
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TestController : ControllerBase
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
    public TestController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
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
        Summary = "Tạo bài kiểm tra mới",
        Description = "Cần cấp quyền Admin cho API"
    )]
    public async Task<TestInsertResponse> InsertTest(TestInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TestInsertCommand, TestInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new TestInsertResponse());
    }
    
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy bài kiểm tra gồm các quiz mà student chọn",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<TestSelectResponse> GetTest([FromQuery] TestSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<TestSelectQuery, TestSelectResponse, TestSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new TestSelectResponse());
    }
}