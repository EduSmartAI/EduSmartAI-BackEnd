using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// StudentSurveyController - Manage student surveys
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class StudentSurveyController : ControllerBase
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
    public StudentSurveyController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
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
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lưu câu trả lời phần khảo sát của học sinh",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentSurveyInsertResponse> InsertStudentSurvey([FromBody] StudentSurveyInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<StudentSurveyInsertCommand, StudentSurveyInsertResponse, Guid?>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new StudentSurveyInsertResponse());
    }
    
    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Hiển câu trả lời phần khảo sát của học sinh",
        Description = "Cần cấp quyền cho API"
    )]
    public async Task<StudentSurveySelectResponse> SelectStudentSurvey([FromQuery] StudentSurveySelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<StudentSurveySelectQuery, StudentSurveySelectResponse, List<StudentSurveySelectResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new StudentSurveySelectResponse());
    }
    
    /// <summary>
    /// Select detail of a student survey
    /// </summary>
    /// <param name="studentSurveyId"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy chi tiết câu trả lời của học sinh trong bài khảo sát",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentSurveySelectDetailResponse> SelectStudentSurveyDetail([FromQuery] Guid studentSurveyId)
    {
        var query = new StudentSurveySelectDetailQuery { StudentSurveyId = studentSurveyId };
        return await ApiControllerHelper.HandleRequest<StudentSurveySelectDetailQuery, StudentSurveySelectDetailResponse, StudentSurveySelectDetailResponseEntity>(
            query,
            _logger,
            ModelState,
            async () => await _mediator.Send(query),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new StudentSurveySelectDetailResponse());
    }
}