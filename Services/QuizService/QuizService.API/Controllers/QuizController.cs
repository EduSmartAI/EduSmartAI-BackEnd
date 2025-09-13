using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.Quizzes.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// QuizController - Manage quizzes
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class QuizController : ControllerBase
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
    public QuizController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Update question and its answers
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách quiz của một bài kiểm tra",
        Description = "Cần cấp quyền cho API"
    )]
    public async Task<QuizSelectsResponse> SelectQuizzes()
    {
        var request = new QuizSelectsQuery();
        return await ApiControllerHelper.HandleRequest<QuizSelectsQuery, QuizSelectsResponse, List<QuizSelectsResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new QuizSelectsResponse());
    }
}