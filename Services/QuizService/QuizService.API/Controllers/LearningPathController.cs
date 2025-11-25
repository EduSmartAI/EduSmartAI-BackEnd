using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.LearningPaths;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// LearningPathController - Manage learning paths
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class LearningPathController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;
    private readonly IdentityEntity? _identityEntity;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="identityService"></param>
    /// <param name="httpContextAccessor"></param>
    public LearningPathController(IMediator mediator, IIdentityService identityService,
        IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
        _identityEntity = null;
    }

    /// <summary>
    /// Create learning path from previous survey and transcript (without doing test)
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Tạo learning path từ khảo sát đã làm trước đó và bảng điểm (không làm bài test)",
        Description =
            "Dành cho sinh viên đã làm khảo sát và không muốn làm bài test. Hệ thống sẽ tính toán level từ bảng điểm. Cần cấp quyền Student cho API"
    )]
    public async Task<InsertLearningPathWithPreviousSurveyAndTranscriptResponse> InsertLearningPathWithPreviousSurveyAndTranscript([FromBody] InsertLearningPathWithPreviousSurveyAndTranscriptCommand request)
    {
        return await ApiControllerHelper
            .HandleRequest<InsertLearningPathWithPreviousSurveyAndTranscriptCommand,
                InsertLearningPathWithPreviousSurveyAndTranscriptResponse,
                InsertLearningPathWithPreviousSurveyAndTranscriptResponseEntity>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity!,
                _httpContextAccessor,
                new InsertLearningPathWithPreviousSurveyAndTranscriptResponse { Success = false });
    }
}

