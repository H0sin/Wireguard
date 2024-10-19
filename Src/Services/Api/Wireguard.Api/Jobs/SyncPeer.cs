using System.Net;
using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Quartz;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

[DisallowConcurrentExecution]
public class SyncPeer(IBus bus, ILogger<SyncPeer> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("sync peer job started.....");
        await bus.Publish(new SyncPeerEvent());
        logger.LogInformation("published sync peer event");
    }
}