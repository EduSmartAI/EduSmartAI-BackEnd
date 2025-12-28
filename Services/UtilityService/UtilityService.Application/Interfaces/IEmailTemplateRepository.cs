using UtilityService.Domain.Models;

namespace UtilityService.Application.Interfaces;

public interface IEmailTemplateRepository
{
    Task<VwVerifyaccount?> GetVerifyUserEmailTemplateAsync();
    
    IEnumerable<Systemconfig> GetSystemConfigs();
}