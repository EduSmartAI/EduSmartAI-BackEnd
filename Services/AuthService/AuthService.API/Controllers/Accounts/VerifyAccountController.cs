using AuthService.Application.Accounts.Commands.Verifies;
using AuthService.API.AbstractControllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace AuthService.API.Controllers.Accounts;

/// <summary>
/// VerifyAccountController - Verify account when create account
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class VerifyAccountController : AbstractApiAsyncControllerNotToken<AccountVerifyCommand, AccountVerifyResponse, string>
{
    private readonly IMediator _mediator;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    public VerifyAccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public override async Task<AccountVerifyResponse> ProcessRequest([FromBody] AccountVerifyCommand request)
    {
        return await ProcessRequest(request, _logger, new AccountVerifyResponse());
    }

    /// <summary>
    /// Main processing
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected override async Task<AccountVerifyResponse> Exec(AccountVerifyCommand request)
    {
        return await _mediator.Send(request);
    }
}