using System.Security.Claims;
using AuthService.Application.Dtos;

namespace AuthService.Application.Interfaces.TokenServices;

public interface ITokenService
{
    /// <summary>
    /// Generate a new ClaimsPrincipal for the user based on the UserLoginDto.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<ClaimsPrincipal> GenerateClaimsPrincipal(UserLoginDto user);

    /// <summary>
    /// Generate a new ClaimsPrincipal for the user based on the ClaimsPrincipal.
    /// </summary>
    /// <param name="claimsPrincipal"></param>
    /// <returns></returns>
    Task<ClaimsPrincipal> GenerateClaimsPrincipal(ClaimsPrincipal claimsPrincipal);

    /// <summary>
    /// Generate a new access token for the user.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    Task<(bool Success, string Message)> LogoutAsync(ClaimsPrincipal user, string accessToken);
    
    /// <summary>
    /// Retrieves the current user's session details
    /// </summary>
    /// <returns></returns>
    TokenVerifyResponse VerifyToken();
}