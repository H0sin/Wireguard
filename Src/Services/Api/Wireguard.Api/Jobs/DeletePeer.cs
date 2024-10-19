using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Quartz;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

[DisallowConcurrentExecution]
public class DeletePeer(IBus bus, ILogger<DeletePeer> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("started delete peer job.....");

        bus.Publish(new DeletePeerEvent());

        logger.LogInformation("delete peer job published.....");
    }
}