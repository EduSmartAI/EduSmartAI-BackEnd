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
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Tạo khảo sát mới",
        Description = "Tạo khảo sát mới với các câu hỏi và câu trả lời tương ứng"
    )]
    public async Task<SurveyInsertResponse> InsertSurvey(SurveyInsertCommand request)
    {
        var response = new SurveyInsertResponse { Success = false };
        var detailErrors = new List<DetailError>();
        
        // Validate that each question of type 2 (multiple choice) has at least one answer
        foreach (var question in request.Questions)
        {
            if ((question.QuestionType == (short) ConstantEnum.QuestionType.MultipleChoice || 
                 question.QuestionType == (short) ConstantEnum.QuestionType.TrueFalse) 
                && (question.Answers == null || !question.Answers.Any()))
            {
                var detailEror = new DetailError();
                detailEror.SetMessage(MessageId.E10000);
                detailEror.ErrorMessage = "Câu hỏi loại trắc nghiệm phải có ít nhất một câu trả lời";
                detailErrors.Add(detailEror);
                
                response.SetMessage(MessageId.E10000);
                response.DetailErrors = detailErrors;
                return response;
            }
        }
        
        // Call the helper to handle the request
        response = await ApiControllerHelper.HandleRequest<SurveyInsertCommand, SurveyInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SurveyInsertResponse());
        return response;
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