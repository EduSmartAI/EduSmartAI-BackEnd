using AuthService.Application.Accounts.Commands.Verifies;
using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace AuthService.API.Controllers.Accounts;

/// <summary>
/// VerifyAccountController - Verify account when create account
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class VerifyAccountController : ControllerBase, IApiAsyncController<AccountVerifyCommand, AccountVerifyResponse>
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
    public async Task<AccountVerifyResponse> ProcessRequest([FromBody] AccountVerifyCommand request)
    {
        return await ApiControllerHelperNotAuth.HandleRequest<AccountVerifyCommand, AccountVerifyResponse, string>(
            request,
            _logger,
            new AccountVerifyResponse(),
            ModelState,
            async () => await _mediator.Send(request)
        );
    }
}