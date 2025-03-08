using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Wireguard.Api.Consumer;

public class ActionPeerConsumer(IConfiguration configuration, ILogger<ActionPeerConsumer> logger)
    : IConsumer<ActionPeerEvent>
{
    public async Task Consume(ConsumeContext<ActionPeerEvent> context)
    {
        var action = context.Message.Action;
        try
        {
            await action;
        }
        catch (Exception e)
        {
            logger.LogError(e, "when run action peer {} occerd erorr", action);
        }
    }
}