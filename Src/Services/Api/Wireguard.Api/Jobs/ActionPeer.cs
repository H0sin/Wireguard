using System.Security.Cryptography.X509Certificates;
using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Quartz;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Jobs;

[DisallowConcurrentExecution]
public class ActionPeer(IBus bus, ILogger<ActionPeer> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Action peer job started.....");
        await bus.Publish(new ActionPeerEvent());
        logger.LogInformation("published action peer event");
    }
}