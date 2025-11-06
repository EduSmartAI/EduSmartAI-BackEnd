using BaseService.API.BaseControllers;
using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.PracticeTest;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PracticeTestController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;
    private readonly IdentityEntity _identityEntity;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PracticeTestController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Select all practice tests
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách bài tập thực hành", Description = "Cần cấp quyền cho API")]
    public async Task<PracticeTestSelectsResponse> SelectPracticeTests()
    {
        var request = new PracticeTestSelectsRequest();
        return await ApiControllerHelper.HandleRequest<PracticeTestSelectsRequest, PracticeTestSelectsResponse, PagedResult<PracticeTestSelectsResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new PracticeTestSelectsResponse());
    }
    
    /// <summary>
    /// Select practice tests detail
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách bài tập thực hành", Description = "Cần cấp quyền cho API")]
    public async Task<PracticeTestSelectResponse> SelectPracticeTestDetail([FromQuery]PracticeTestSelectRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestSelectRequest, PracticeTestSelectResponse, PracticeTestSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new PracticeTestSelectResponse());
    }
    
    /// <summary>
    /// Select code languages for practice tests
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách các ngôn ngữ lập trình hỗ trợ cho bài tập thực hành", Description = "Cần cấp quyền cho API")]
    public async Task<PracticeTestLanguageSelectsResponse> SelectCodeLanguages()
    {
        var request = new PracticeTestLanguageSelectsRequest();
        return await ApiControllerHelper.HandleRequest<PracticeTestLanguageSelectsRequest, PracticeTestLanguageSelectsResponse, List<PracticeTestLanguageSelectsResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new PracticeTestLanguageSelectsResponse());
    }
    
    /// <summary>
    /// Insert practice test submit
    /// </summary>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Nộp bài tập thực hành", Description = "Cần cấp quyền cho API")]
    public async Task<PracticeTestSubmitInsertResponse> SubmitPracticeTest([FromBody] PracticeTestSubmitInsertRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestSubmitInsertRequest, PracticeTestSubmitInsertResponse, PracticeTestSubmitInsertResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new PracticeTestSubmitInsertResponse());
    }
}