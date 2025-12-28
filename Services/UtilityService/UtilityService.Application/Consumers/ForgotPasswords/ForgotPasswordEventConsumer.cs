using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.AuthService;
using MassTransit;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Consumers.ForgotPasswords;

public class ForgotPasswordEventConsumer(ICommandRepository<Emailtemplate> repository, ICommandRepository<Systemconfig> sytemConfigRepo) : IConsumer<ForgotPasswordEvent>
{
    public async Task Consume(ConsumeContext<ForgotPasswordEvent> context)
    {
        var evt = context.Message;
        
        await ForgotPasswordSendMail.SendMailVerifyInformation(repository, sytemConfigRepo, evt.Email, evt.Key, new List<DetailError>());
    }
}