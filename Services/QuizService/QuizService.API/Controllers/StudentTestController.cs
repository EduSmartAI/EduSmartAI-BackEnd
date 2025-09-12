using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.StudentTests.Commands;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Applications.Tests.Commands;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// StudentTestController - Controller for managing student tests.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class StudentTestController : ControllerBase
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
    public StudentTestController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Insert new answer of student
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lưu câu trả lời của học sinh",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentTestInsertResponse> InsertStudentTest(StudentTestInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<StudentTestInsertCommand, StudentTestInsertResponse, Guid?>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new StudentTestInsertResponse());
    }

    /// <summary>
    /// Select answers of student in a test
    /// </summary>
    /// <param name="studentTestId"></param>
    /// <returns></returns>
    [HttpGet()]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy câu trả lời của học sinh trong bài test",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentTestSelectResponse> SelectStudentTest([FromQuery] Guid studentTestId)    {
        var query = new StudentTestSelectQuery { StudentTestId = studentTestId };
        return await ApiControllerHelper.HandleRequest<StudentTestSelectQuery, StudentTestSelectResponse, StudentTestSelectResponseEntity>(
            query,
            _logger,
            ModelState,
            async () => await _mediator.Send(query),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new StudentTestSelectResponse());
    }
}