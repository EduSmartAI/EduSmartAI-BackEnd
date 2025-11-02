using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
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
    [Authorize(Roles = ConstRole.Student,
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật thông tin học sinh",
        Description = "Cần cấp quyền Student"
    )]
    public async Task<StudentProfileUpdateResponse> UpdateStudentProfile([FromBody] StudentProfileUpdateCommand request)
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
    [Authorize(Roles = ConstRole.Student,
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Hiển thị profile học sinh",
        Description = "Cần cấp quyền Student"
    )]
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
    
}