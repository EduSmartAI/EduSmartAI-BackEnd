using BaseService.Application.Interfaces.Repositories;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;

namespace UtilityService.Infrastructure.Implements;

public class SendmailService : ISendmailService
{

    public SendmailService()
    {
    }

    public async Task VerifyEmail(string key, string email)
    {
        
    }
}