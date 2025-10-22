using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.SuggestCourses.Commands.AcceptCourseSuggestion;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

/// <summary>
/// Controller for course suggestions
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
public class CourseSuggestionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;
    private readonly IdentityEntity _identityEntity;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CourseSuggestionsController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _identityService = identityService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Accept a course suggestion
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Chấp nhận đề xuất khóa học",
        Description = "API cho phép sinh viên chấp nhận đề xuất khóa học từ hệ thống. Sau khi chấp nhận, IsAccepted sẽ được đặt thành true."
    )]
    public async Task<AcceptCourseSuggestionResponse> UpdateAcceptCourseSuggestionStatus([FromBody] AcceptCourseSuggestionCommand request)
    {
        return await ApiControllerHelper.HandleRequest<AcceptCourseSuggestionCommand, AcceptCourseSuggestionResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new AcceptCourseSuggestionResponse());
    }
}

