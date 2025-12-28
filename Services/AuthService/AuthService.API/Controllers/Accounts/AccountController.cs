using AuthService.Application.Accounts.Commands.ForgotPassword;
using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Accounts.Commands.UpdatePassword;
using AuthService.Application.Accounts.Commands.Verifies;
using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Swashbuckle.AspNetCore.Annotations;

namespace AuthService.API.Controllers.Accounts;

/// <summary>
/// AccountController - Manage accounts
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AccountController : ControllerBase
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("insert-account")]
    public async Task<AccountInsertResponse> InsertStudent(AccountInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<AccountInsertCommand, AccountInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            new AccountInsertResponse()
        );
    }
    
    
    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("verify-account")]
    public async Task<AccountVerifyResponse> VerifyAccount([FromBody] AccountVerifyCommand request)
    {
        return await ApiControllerHelper.HandleRequest<AccountVerifyCommand, AccountVerifyResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            new AccountVerifyResponse()
        );
    }
    
    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("forgot-password")]
    public async Task<ForgotPasswordResponse> ForgotPassword([FromBody] ForgotPasswordCommand request)
    {
        return await ApiControllerHelper.HandleRequest<ForgotPasswordCommand, ForgotPasswordResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            new ForgotPasswordResponse()
        );
    } 
    
    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("reset-password")]
    public async Task<ResetPasswordResponse> ResetPassword([FromBody] ResetPasswordCommand request)
    {
        return await ApiControllerHelper.HandleRequest<ResetPasswordCommand, ResetPasswordResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            new ResetPasswordResponse()
        );
    }
    
    /// <summary>
    /// Update password for authenticated user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Cập nhật mật khẩu")]  
    [HttpPost("update-password")]
    public async Task<UpdatePasswordResponse> UpdatePassword([FromBody] UpdatePasswordCommand request)
    {
        return await ApiControllerHelper.HandleRequest<UpdatePasswordCommand, UpdatePasswordResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            new UpdatePasswordResponse()
        );
    }
}