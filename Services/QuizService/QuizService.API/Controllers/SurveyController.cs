using BaseService.API.BaseControllers;
using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// SurveyController - Manage surveys
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SurveyController : ControllerBase
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
    public SurveyController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }
    
    /// <summary>
    /// Incoming Get Select
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách các khảo sát",
        Description = "Lấy danh sách các khảo sát"
    )]
    public async Task<SurveySelectsResponse> SelectSurvey()
    {
        var request = new SurveySelectsQuery();
        return await ApiControllerHelper.HandleRequest<SurveySelectsQuery, SurveySelectsResponse, List<SurveySelectsResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SurveySelectsResponse());
    }
    
    /// <summary>
    /// Incoming Get Details with pagination
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("Detail")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách các khảo sát",
        Description = "Lấy chi tiết khảo sát với phân trang"
    )]
    public async Task<SurveyDetailSelectResponse> SelectSurveyDetailAsync([FromQuery] SurveyDetailSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<SurveyDetailSelectQuery, SurveyDetailSelectResponse, PagedResult<SurveyDetailSelectResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SurveyDetailSelectResponse());
    }
}