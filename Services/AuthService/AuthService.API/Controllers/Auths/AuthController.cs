using AuthService.Application.Accounts.Commands.Logins;
using AuthService.Application.Dtos;
using AuthService.Application.Interfaces.TokenServices;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace AuthService.API.Controllers.Auths;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenService"></param>
    /// <param name="mediator"></param>
    public AuthController(ITokenService tokenService, IMediator mediator)
    {
        _tokenService = tokenService;
        _mediator = mediator;
    }

    /// <summary>
    /// Exchange token
    /// </summary>
    /// <returns></returns>
    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var openIdRequest = HttpContext.GetOpenIddictServerRequest();
        
        var request = new UserLoginCommand(
            UserName: openIdRequest!.Username,
            Password: openIdRequest.Password
        );

        // Password
        if (openIdRequest.IsPasswordGrantType())
        {
            return await TokensForPasswordGrantType(request);
        }

        // Refresh token
        if (openIdRequest.IsRefreshTokenGrantType())
        {
            return await TokensForRefreshTokenGrantType();
        }

        // Unsupported grant type
        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType
        });
    }

    /// <summary>
    /// Logout endpoint - revoke tokens properly
    /// </summary>
    /// <returns></returns>
    [HttpPost("~/connect/logout")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Get the access token from the request headers
            var accessToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            // If no access token is provided, return an error
            var result = await _tokenService.LogoutAsync(User, accessToken!);
            if (!result.Success)
            {
                return BadRequest(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidRequest,
                    ErrorDescription = result.Message
                });
            }

            // Logout the user from OpenIddict
            await HttpContext.SignOutAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            return Ok(new
            {
                Success = true, result.Message
            });
        }
        catch
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.ServerError,
                ErrorDescription = "An error occurred while logging out."
            });
        }
    }

    [HttpGet("/session")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public UserSessionResponse UserSession()
    {
        return _tokenService.UserSession();
    }

    /// <summary>
    /// Handle refresh token grant type
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task<IActionResult> TokensForPasswordGrantType(UserLoginCommand request)
    {
        var loginResult = await _mediator.Send(request);
        if (!loginResult.Success)
        {
            var errorResponse = new OpenIddictResponse();

            if (loginResult.MessageId == MessageId.E11001)
            {
                errorResponse.Error = OpenIddictConstants.Errors.InvalidClient;
                errorResponse.ErrorDescription = loginResult.Message;
            }
            else if (loginResult.MessageId == MessageId.E11002)
            {
                errorResponse.Error = OpenIddictConstants.Errors.InvalidRequest;
                errorResponse.ErrorDescription = loginResult.Message;
            }
            else if (loginResult.MessageId == MessageId.E11003)
            {
                errorResponse.Error = OpenIddictConstants.Errors.AccessDenied;
                errorResponse.ErrorDescription = loginResult.Message;
            }
            else
            {
                errorResponse.Error = OpenIddictConstants.Errors.InvalidGrant;
                errorResponse.ErrorDescription = "An unexpected error occurred during login.";
            }

            return BadRequest(errorResponse);
        }

        var loginResponse = loginResult.Response;

        var userLoginDto = new UserLoginDto
        {
            UserId = loginResponse.UserId,
            Email = loginResponse.Email,
            FullName = loginResponse.FullName,
            RoleName = loginResponse.RoleName,
        };

        var claimsPrincipal = await _tokenService.GenerateClaimsPrincipal(userLoginDto);

        // Generate access and refresh tokens
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handle refresh token grant type
    /// </summary>
    /// <returns></returns>
    private async Task<IActionResult> TokensForRefreshTokenGrantType()
    {
        try
        {
            // Authenticate the refresh token
            var authenticateResult =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                return Unauthorized(new OpenIddictResponse
                {
                    Error = OpenIddictConstants.Errors.InvalidGrant,
                    ErrorDescription = "The refresh token is invalid."
                });
            }

            var claimsPrincipal = authenticateResult.Principal;
            var newClaimsPrincipal = await _tokenService.GenerateClaimsPrincipal(claimsPrincipal);

            return SignIn(newClaimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.ServerError,
                ErrorDescription = "An error occurred while processing the refresh token."
            });
        }
    }
}