using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Students.Commands.Updates;
using StudentService.Application.Applications.Students.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

[Route("api/v1/[controller]")]
public class StudentController(IIdentityService identityService, IMediator mediator, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IdentityEntity _identityEntity;

    /// <summary>
    /// Update student profile
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Cập nhật thông tin học sinh", Description = "Cần cấp quyền Student")]
    public async Task<StudentProfileUpdateResponse> UpdateStudentProfile([FromForm] StudentProfileUpdateCommand request)
    {
        return await ApiControllerHelper.HandleRequest<StudentProfileUpdateCommand, StudentProfileUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentProfileUpdateResponse()
        );
    }
    
    /// <summary>
    /// Select student profile
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Hiển thị profile học sinh", Description = "Cần cấp quyền Student")]
    public async Task<StudentProfileSelectResponse> SelectStudentProfile()
    {
        var request = new StudentProfileSelectQuery();
        return await ApiControllerHelper.HandleRequest<StudentProfileSelectQuery, StudentProfileSelectResponse, StudentProfileSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentProfileSelectResponse()
        );
    }
    
    /// <summary>
    /// Insert student transcript
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Import bảng điểm từ FAP cho sinh viên", Description = "Cần cấp quyền Student")]
    public async Task<StudentTranscriptInsertResponse> InsertStudentTranscript([FromForm] StudentTranscriptInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<StudentTranscriptInsertCommand, StudentTranscriptInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentTranscriptInsertResponse()
        );
    }
    
    /// <summary>
    /// Select student transcript
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Hiển thị bảng điểm được import từ FAP của sinh viên", Description = "Cần cấp quyền Student")]
    public async Task<StudentTranscriptSelectResponse> SelectStudentTranscript()
    {
        var request = new StudentTranscriptSelectQuery();
        return await ApiControllerHelper.HandleRequest<StudentTranscriptSelectQuery, StudentTranscriptSelectResponse, List<StudentTranscriptSelectResponseEntity>>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentTranscriptSelectResponse()
        );
    }
    
    /// <summary>
    /// Select student technologies and learning goals
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Hiển thị công nghệ và mục tiêu học tập của sinh viên", Description = "Cần cấp quyền Student")]
    public async Task<StudentTechnologyGoalSelectResponse> SelectStudentTechnologyGoal()
    {
        var request = new StudentTechnologyGoalSelectQuery();
        return await ApiControllerHelper.HandleRequest<StudentTechnologyGoalSelectQuery, StudentTechnologyGoalSelectResponse, StudentTechnologyGoalSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new StudentTechnologyGoalSelectResponse()
        );
    }
}