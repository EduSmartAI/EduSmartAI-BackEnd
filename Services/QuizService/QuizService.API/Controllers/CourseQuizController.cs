using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Applications.QuizCourses.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// CourseQuizController - Manage course quiz and its student answers
/// </summary>
/// <param name="mediator"></param>
/// <param name="identityService"></param>
/// <param name="httpContextAccessor"></param>
[ApiController]
[Route("api/v1/[controller]")]
public class CourseQuizController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor) : ControllerBase
{

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IdentityEntity _identityEntity;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lưu câu trả lời của sinh viên trong bài kiếm tra course",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentQuizCourseInsertResponse> InsertStudentQuizCourse(StudentQuizCourseInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<StudentQuizCourseInsertCommand, StudentQuizCourseInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentQuizCourseInsertResponse());
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Hiển thị bài kiểm tra course cho sinh viên",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentCourseQuizSelectResponse> SelectStudentQuizCourse(StudentCourseQuizSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<StudentCourseQuizSelectQuery, StudentCourseQuizSelectResponse, StudentCourseQuizSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentCourseQuizSelectResponse());
    }

}