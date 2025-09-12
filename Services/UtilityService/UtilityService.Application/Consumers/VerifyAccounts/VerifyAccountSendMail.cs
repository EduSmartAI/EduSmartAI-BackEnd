using BaseService.Common.ApiEntities;
using UtilityService.Application.Interfaces;
using UtilityService.Application.Logics;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Consumers.VerifyAccounts;

/// <summary>
/// VerifyAccountSendMail - Send mail to verify the user registration
/// </summary>
public static class VerifyAccountSendMail
{
    /// <summary>
    /// Send mail to verify the user registration 
    /// </summary>
    /// <param name="emailTemplateRepository"></param>
    /// <param name="email"></param>
    /// <param name="key"></param>
    /// <param name="detailErrors"></param>
    /// <returns></returns>
    public static async Task<bool> SendMailVerifyInformation(IEmailTemplateRepository emailTemplateRepository, string email, string key, List<DetailError> detailErrors)
    {
        // Get the mail template
        var mailTemplate = await emailTemplateRepository.GetVerifyUserEmailTemplateAsync();
        var mailTitle = mailTemplate!.Title.Replace("${title}", mailTemplate.Title);
        
        var encodedKey = Uri.EscapeDataString(key);
        // Replace the variables in the mail template
        var replacements = new Dictionary<string, string>
        {
            { "${key}", encodedKey }
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
        return await SendMailLogic.SendMail(mailInfo, email, emailTemplateRepository, detailErrors);
    }
}