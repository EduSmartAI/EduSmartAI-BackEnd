using BaseService.Common.ApiEntities;

namespace BaseService.Application.Interfaces.Commons;

public record DecryptTextEmailAndIdResponse : AbstractApiResponse<DecryptTextResponseEntity>
{
    public override DecryptTextResponseEntity Response { get; set; }
}

public class DecryptTextResponseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}