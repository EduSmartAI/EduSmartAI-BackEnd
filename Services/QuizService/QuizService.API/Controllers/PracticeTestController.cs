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
    /// Select 3 random practice tests with all difficulty levels
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy 3 bài tập thực hành ngẫu nhiên", Description = "Lấy 3 bài tập thực hành ngẫu nhiên có đủ 3 level: Dễ (Easy), Trung bình (Medium), Khó (Hard). Cần cấp quyền cho API")]
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
    /// Select user template code for practice test
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy source code mẫu cho user tương ứng với problem và ngôn ngữ lập trình tương ứng",
        Description = "API này trả về hàm mẫu dành cho người dùng, dựa trên bài tập (problem) và ngôn ngữ lập trình được chọn. Người dùng có thể sử dụng mã nguồn này để code phần bài tập thực hành của mình."
    )]
    public async Task<PracticeTestUserTemplateCodeSelectResponse> SelectUserTemplateCode([FromQuery] PracticeTestUserTemplateCodeSelectRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestUserTemplateCodeSelectRequest, PracticeTestUserTemplateCodeSelectResponse, PracticeTestUserTemplateCodeSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new PracticeTestUserTemplateCodeSelectResponse());
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
    
    /// <summary>
    /// Check practice test code with multiple custom inputs without saving to database
    /// </summary>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Kiểm tra code với nhiều input tự nhập mà không lưu vào database", 
        Description = "API này cho phép student kiểm tra code của mình với nhiều test cases (inputs) cùng lúc để xem kết quả thực thi trước khi nộp bài chính thức. Student có thể gửi 1 hoặc nhiều inputs trong một request. Kết quả trả về bao gồm status, output, lỗi (nếu có), thời gian thực thi và memory cho từng test case mà không lưu vào database. Cần cấp quyền cho API")]
    public async Task<PracticeTestCodeCheckResponse> CheckPracticeTestCode([FromBody] PracticeTestCodeCheckRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestCodeCheckRequest, PracticeTestCodeCheckResponse, PracticeTestCodeCheckResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new PracticeTestCodeCheckResponse());
    }
}