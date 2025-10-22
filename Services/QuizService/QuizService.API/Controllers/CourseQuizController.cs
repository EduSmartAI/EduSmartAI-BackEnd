using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;
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
    /// Update quiz course
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật bài kiểm tra cho khoá học",
        Description = "Cần cấp quyền Teacher cho API. Chỉ cập nhật thông tin quiz settings và câu hỏi/câu trả lời hiện có"
    )]
    public async Task<QuizCourseUpdateResponse> UpdateQuizCourse([FromBody] QuizCourseUpdateCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuizCourseUpdateCommand, QuizCourseUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new QuizCourseUpdateResponse());
    }
    
    /// <summary>
    /// Add new course quiz
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [SwaggerOperation(
        Summary = "API dùng để test thêm mới bài kiểm tra cho khoá học"
    )]
    public async Task<QuizCourseInsertResponse> InsertQuizCourse([FromBody] QuizCourseInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuizCourseInsertCommand, QuizCourseInsertResponse, QuizCourseInsertResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new QuizCourseInsertResponse());
    }
    
    /// <summary>
    /// Add new course quiz
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [SwaggerOperation(
        Summary = "API dùng để test lấy bài kiểm tra cho khoá học"
    )]
    public async Task<QuizCourseSelectQueryResponse> SelectQuizCourse([FromQuery] QuizCourseSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<QuizCourseSelectQuery, QuizCourseSelectQueryResponse, QuizCourseSelectQueryResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new QuizCourseSelectQueryResponse());
    }
    
    /// <summary>
    /// Add new questions to existing course quiz
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Thêm câu hỏi mới vào bài kiểm tra",
        Description = "Cần cấp quyền Teacher cho API"
    )]
    public async Task<QuizCourseAddQuestionsResponse> InsertQuestionsToQuizCourse([FromBody] QuizCourseAddQuestionsCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuizCourseAddQuestionsCommand, QuizCourseAddQuestionsResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new QuizCourseAddQuestionsResponse());
    }
    
    /// <summary>
    /// Delete questions from quiz (soft delete)
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Xóa câu hỏi khỏi bài kiểm tra",
        Description = "Cần cấp quyền Teacher cho API. Xóa mềm (soft delete) - set IsActive = false"
    )]
    public async Task<QuizCourseDeleteQuestionsResponse> DeleteQuestionsFromQuizCourse([FromBody] QuizCourseDeleteQuestionsCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuizCourseDeleteQuestionsCommand, QuizCourseDeleteQuestionsResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new QuizCourseDeleteQuestionsResponse());
    }
    
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
        return await ApiControllerHelper.HandleRequest<StudentQuizCourseInsertCommand, StudentQuizCourseInsertResponse, StudentQuizCourseInsertResponseEntity>(
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
    /// Check student quiz attempt
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Hiển thị bài kiểm tra course cho sinh viên",
        Description = "Cần cấp quyền Student cho API"
    )]
    public async Task<StudentCourseQuizSelectResponse> SelectStudentQuizCourse([FromQuery] StudentCourseQuizSelectQuery request)
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

    /// <summary>
    /// Check student quiz attempt
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Kiểm tra bài kiểm tra course của sinh viên",
        Description = "Cần cấp quyền Student cho API"
    )]
    
    public async Task<QuizCourseCheckAttemptResponse> CheckStudentQuizAttempt([FromBody] QuizCourseCheckAttemptCommand request)
    {
        return await ApiControllerHelper.HandleRequest<QuizCourseCheckAttemptCommand, QuizCourseCheckAttemptResponse, QuizCourseCheckAttemptEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new QuizCourseCheckAttemptResponse());
	}

}