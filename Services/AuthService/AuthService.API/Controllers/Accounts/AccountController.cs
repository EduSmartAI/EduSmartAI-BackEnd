using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Accounts.Commands.Verifies;
using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

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
}