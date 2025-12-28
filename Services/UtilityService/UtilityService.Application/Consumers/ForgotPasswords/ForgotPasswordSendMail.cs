using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.ApiEntities;
using UtilityService.Application.Logics;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Consumers.ForgotPasswords;

/// <summary>
/// VerifyAccountSendMail - Send mail to verify the user registration
/// </summary>
public static class ForgotPasswordSendMail
{
    /// <summary>
    /// Send mail to verify the user registration 
    /// </summary>
    /// <param name="emailTemplateRepository"></param>
    /// <param name="systemConfigRepository"></param>
    /// <param name="email"></param>
    /// <param name="key"></param>
    /// <param name="detailErrors"></param>
    /// <returns></returns>
    public static async Task<bool> SendMailVerifyInformation(ICommandRepository<Emailtemplate> emailTemplateRepository, ICommandRepository<Systemconfig> systemConfigRepository, string email, string key, List<DetailError> detailErrors)
    {
        // Get the mail template
        var mailTemplate = await emailTemplateRepository.FirstOrDefaultAsync(x => x.ScreenName == "ForgotPassword" && x.IsActive);
        var mailTitle = mailTemplate!.Title.Replace("${title}", mailTemplate.Title);
        
        var encodedKey = Uri.EscapeDataString(key);
        // Replace the variables in the mail template
        var replacements = new Dictionary<string, string>
        {
            { "${forgot_key}", encodedKey }
        };
        
        // Replace the variables in the mail template
        var mailBody = mailTemplate.Body;
        foreach (var replacement in replacements)
        {
            mailBody = mailBody.Replace(replacement.Key, replacement.Value);
        }
        
        // Send the mail
        var mailInfo = new Emailtemplate
        {
            Title = mailTitle,
            Body = mailBody,
        };
        return await SendMailLogic.SendMail(mailInfo, email, systemConfigRepository, detailErrors);
    }
}