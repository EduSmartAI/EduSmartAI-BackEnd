using Microsoft.EntityFrameworkCore;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;
using UtilityService.Infrastructure.Contexts;

namespace UtilityService.Infrastructure.Implements;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly UtilityServiceContext _context;

    public EmailTemplateRepository(UtilityServiceContext appDbContext)
    {
        _context = appDbContext;
    }

    public async Task<VwVerifyaccount?> GetVerifyUserEmailTemplateAsync()
    {
        return await _context.VwVerifyaccounts.FirstOrDefaultAsync();
    }

    public IEnumerable<Systemconfig> GetSystemConfigs()
    {
        return _context.Systemconfigs;
    }
}