using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using UtilityService.Application.Interfaces;

namespace UtilityService.Application.Consumers.VerifyAccounts;

public class SendKeyEventConsumer : IConsumer<SendKeyEvent>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;

    public SendKeyEventConsumer(IEmailTemplateRepository emailTemplateRepository)
    {
        _emailTemplateRepository = emailTemplateRepository;
    }

    public async Task Consume(ConsumeContext<SendKeyEvent> context)
    {
        var key = context.Message.Key;
        var email = context.Message.Email;
        
        await VerifyAccountSendMail.SendMailVerifyInformation(_emailTemplateRepository, email, key, new List<DetailError>());
    }
}