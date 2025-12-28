namespace UtilityService.Application.Interfaces;

public interface ISendmailService
{
    Task VerifyEmail(string key, string email);
}