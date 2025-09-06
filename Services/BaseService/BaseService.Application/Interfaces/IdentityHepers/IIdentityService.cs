using System.Security.Claims;

namespace BaseService.Application.Interfaces.IdentityHepers;

public interface IIdentityService
{
    IdentityEntity? GetIdentity(ClaimsPrincipal user);
    
    IdentityEntity? GetCurrentUser();
}

