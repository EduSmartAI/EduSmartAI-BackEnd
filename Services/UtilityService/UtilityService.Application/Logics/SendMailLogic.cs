using System.Net;
using System.Net.Mail;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Logics;

/// <summary>
/// Send mail config.
/// </summary>
public static class SendMailLogic
{
    /// <summary>
    ///  Send mail
    /// </summary>
    /// <param name="mailAddress"></param>
    /// <param name="title"></param>
    /// <param name="body"></param>
    public static void Send(string mailAddress, string title, string body, IEmailTemplateRepository emailTemplateRepository)
    {
        var configs = emailTemplateRepository.GetSystemConfigs().ToList();
        // The SMTP server is obtained from the system configuration (SYSTEM_CONFIG).
        var mailSmtp = configs.Find(c => c.Id == SystemConfig.MailSmtp)?.Value;
        // The sender of the email is obtained from the system configuration (SYSTEM_CONFIG).
        var mailFrom = configs.Find(c => c.Id == SystemConfig.MailFrom)?.Value;
        // The BCC email sender is obtained from the system configuration (SYSTEM_CONFIG).
        var mailToBcc = configs.Find(c => c.Id == SystemConfig.MailToBcc)?.Value;
        // The password for the email is obtained from the system configuration (SYSTEM_CONFIG).
        var mailPassword = configs.Find(c => c.Id == SystemConfig.MailPassword)?.Value;
        
        var client = new SmtpClient
        {
            Host = mailSmtp,
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(mailFrom, mailPassword)
        };

        // Generate a message instance and set parameters.
        using (var message = new MailMessage())
        {
            message.From = new MailAddress(mailFrom);
            message.To.Add(mailAddress);
            message.Subject = title;
            message.Body = body;
            message.Bcc.Add(mailToBcc);
            message.IsBodyHtml = true;
            client.Send(message);
        }
    }

    /// <summary>
    /// Send mail
    /// </summary>
    /// <param name="emailTemplate"></param>
    /// <param name="mailAddress"></param>
    /// <param name="detailErrorList"></param>
    /// <returns></returns>
    public static async Task<bool> SendMail(Emailtemplate emailTemplate, string mailAddress, IEmailTemplateRepository emailTemplateRepository, List<DetailError> detailErrorList)
    {
        if (string.IsNullOrEmpty(emailTemplate.Title))
        {
            detailErrorList.Add(new DetailError
            {
                Field = emailTemplate.Title,
                MessageId = "E00000",
                ErrorMessage = "Title is empty"
            });
        }
        if (string.IsNullOrEmpty(emailTemplate.Body))
        {
            detailErrorList.Add(new DetailError()
            {
                Field = emailTemplate.Body,
                MessageId = "E00000",
                ErrorMessage = "Body is empty"
            });
        }
        if (string.IsNullOrEmpty(mailAddress))
        {
            detailErrorList.Add(new DetailError()
            {
                Field = mailAddress,
                MessageId = "E00000",
                ErrorMessage = "Mail address is empty"
            });
        }

        try
        {
            // Send mail
            Send(mailAddress, emailTemplate.Title, emailTemplate.Body, emailTemplateRepository);
            return true;
        }
        catch (Exception e)
        {
            var detailError = new DetailError();
            detailError.SetMessage(MessageId.E99999);
            detailErrorList.Add(detailError);
            return false;
        }
    }
}