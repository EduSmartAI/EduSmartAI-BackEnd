using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.ApiEntities;
using UtilityService.Application.Logics;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Consumers;

/// <summary>
/// PerformanceLearningPathSendMail - Send mail to notify student about PDF report
/// </summary>
public static class PerformanceLearningPathSendMail
{
    /// <summary>
    /// Send mail to notify student about PDF report
    /// </summary>
    /// <param name="emailTemplateRepository"></param>
    /// <param name="systemConfigRepository"></param>
    /// <param name="email"></param>
    /// <param name="fileName"></param>
    /// <param name="pdfUrl"></param>
    /// <param name="expiresMinutes"></param>
    /// <param name="supportEmail"></param>
    /// <param name="detailErrors"></param>
    /// <returns></returns>
    public static async Task<bool> SendMailPdfReady(
        ICommandRepository<Emailtemplate> emailTemplateRepository, 
        ICommandRepository<Systemconfig> systemConfigRepository, 
        string email, 
        string fileName,
        string pdfUrl,
        int expiresMinutes = 30,
        string? supportEmail = null,
        List<DetailError>? detailErrors = null)
    {
        detailErrors ??= new List<DetailError>();
        
        // Get the mail template
        var mailTemplate = await emailTemplateRepository.FirstOrDefaultAsync(x => x.ScreenName == "PerformanceLearningPath" && x.IsActive);
        if (mailTemplate == null)
        {
            detailErrors.Add(new DetailError
            {
                Field = "EmailTemplate",
                MessageId = "E00000",
                ErrorMessage = "Không tìm thấy email template với ScreenName: PerformanceLearningPath"
            });
            return false;
        }
        
        var mailTitle = mailTemplate.Title;
        
        // Replace the variables in the mail template
        var replacements = new Dictionary<string, string>
        {
            { "${file_name}", fileName },
            { "${pdf_url}", pdfUrl },
            { "${expires_minutes}", expiresMinutes.ToString() },
            { "${support_email}", supportEmail ?? "support@edusmart.ai" }
        };
        
        // Replace the variables in the mail template body
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

