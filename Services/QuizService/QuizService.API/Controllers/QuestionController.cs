using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.Questions.Commands;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// QuestionController - Manage question and its answers
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class QuestionController : ControllerBase
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
    public QuestionController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
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
    [HttpPut]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật câu hỏi và câu trả lời",
        Description = "Cần cấp quyền Admin cho API. Update question và tất cả answers của nó"
    )]
    public async Task<QuestionUpdateResponse> UpdateQuestion(QuestionUpdateCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuestionUpdateCommand, QuestionUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new QuestionUpdateResponse());
    }

    /// <summary>
    /// Delete question and its answers
    /// </summary>
    /// <param name="questionId"></param>
    /// <returns></returns>
    [HttpDelete]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Xóa câu hỏi và tất cả câu trả lời",
        Description = "Cần cấp quyền Admin cho API. Delete question và tất cả answers của nó"
    )]
    public async Task<QuestionDeleteResponse> DeleteQuestion(Guid questionId)
    {
        var request = new QuestionDeleteCommand { QuestionId = questionId };
        
        return await ApiControllerHelper.HandleRequest<QuestionDeleteCommand, QuestionDeleteResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new QuestionDeleteResponse());
    }

    /// <summary>
    /// Insert question with answers into quiz
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Thêm câu hỏi và câu trả lời vào quiz",
        Description = "Cần cấp quyền Admin cho API. Insert question và tất cả answers vào quiz"
    )]
    public async Task<QuestionInsertResponse> InsertQuestion(QuestionInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuestionInsertCommand, QuestionInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new QuestionInsertResponse());
    }
}
