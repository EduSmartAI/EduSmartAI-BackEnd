using BaseService.Common.ApiEntities;

namespace BaseService.Application.Interfaces.Commons
{
    public record DecryptTextIdAndDateTimeResponse : AbstractApiResponse<DecryptTextIdAndDateTimeResponseEntity>
    {
        public override DecryptTextIdAndDateTimeResponseEntity Response { get; set; }
    }
 
    public class DecryptTextIdAndDateTimeResponseEntity
    {
        public Guid Id { get; set; }
        public DateTime DateTimeValue { get; set; }
    }
}
