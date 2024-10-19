using EventBus.Messages.Common;
using MassTransit;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Extensions;
using Wireguard.Api.Filters;
using Quartz;
using Quartz.Impl;
using Wireguard.Api.Consumer;
using Wireguard.Api.Jobs;
using Wireguard.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IInterfaceRepository, InterfaceRepository>();
builder.Services.AddScoped<IIpAddressRepository, IpAddressRepository>();
builder.Services.AddScoped<IPeerRepository, PeerRepository>();

builder.Services.AddSingleton<ExceptionHandlerFilter>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ActionPeerConsumer>();
    x.AddConsumer<SyncPeerConsumer>();
    x.AddConsumer<DeletePeerConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetValue<string>("RabbitMQ:Host"), x =>
        {
            x.Username(builder.Configuration.GetValue<string>("RabbitMQ:Username"));
            x.Password(builder.Configuration.GetValue<string>("RabbitMQ:Password"));
        });

        cfg.ReceiveEndpoint(EventBusConstans.ActionPeerQueue, e =>
        {
            e.ConfigureConsumer<ActionPeerConsumer>(context);
            e.PrefetchCount = 1;
        });

        cfg.ReceiveEndpoint(EventBusConstans.SyncPeerQueue, e =>
        {
            e.ConfigureConsumer<SyncPeerConsumer>(context);
            e.PrefetchCount = 1;
        });

        cfg.ReceiveEndpoint(EventBusConstans.DeletePeerQueue, e =>
        {
            e.ConfigureConsumer<DeletePeerConsumer>(context);
            e.PrefetchCount = 1;
        });
    });
});

builder.Services.AddMassTransitHostedService();

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobSettings = builder.Configuration.GetSection("Quartz:Jobs").Get<List<JobSettings>>();

    if (jobSettings != null)
        foreach (var settings in jobSettings)
        {
            var jobType = Type.GetType(settings.JobType);
            if (jobType == null)
            {
                throw new InvalidOperationException($"Job type '{settings.JobType}' could not be found.");
            }

            var jobKey = new JobKey(settings.JobName, settings.JobGroup);

            switch (settings.JobName)
            {
                case "SyncPeer":
                    q.AddJob<SyncPeer>(opts => opts.WithIdentity(jobKey));
                    break;

                case "ActionPeer":
                    q.AddJob<ActionPeer>(opts => opts.WithIdentity(jobKey));
                    break;

                case "DeletePeer":
                    q.AddJob<DeletePeer>(opts => opts.WithIdentity(jobKey));
                    break;
            }

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity(settings.TriggerName, settings.TriggerGroup)
                .WithCronSchedule(settings.CronSchedule));
        }
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

#region c o r s

builder.Services.AddCors(option =>
{
    option.DefaultPolicyName = "master";
    option.AddDefaultPolicy(configure =>
    {
        configure.AllowAnyHeader();
        configure.AllowAnyMethod();
        configure.AllowAnyOrigin();
    });
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseCors("master");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MigrateDatabase<Program>().Run();