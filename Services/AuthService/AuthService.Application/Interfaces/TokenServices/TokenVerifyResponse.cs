using BaseService.Common.ApiEntities;

namespace AuthService.Application.Interfaces.TokenServices;

public record TokenVerifyResponse : AbstractApiResponse<TokenVerifyResponseEntity>
{
    public override TokenVerifyResponseEntity Response { get; set; }
}

public class TokenVerifyResponseEntity
{
    public Guid UserId { get; set; }
    
    public string Name { get; set; }
    
    public string Email { get; set; }
    public string Role { get; set; }
}