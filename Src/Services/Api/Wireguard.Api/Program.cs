using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Extensions;
using Wireguard.Api.Filters;
using Quartz;
using Quartz.Impl;
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
            }

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity(settings.TriggerName, settings.TriggerGroup)
                .WithCronSchedule(settings.CronSchedule));
        }
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// StdSchedulerFactory factory = new StdSchedulerFactory();
// IScheduler scheduler = await factory.GetScheduler();
//
// await scheduler.Start();
//
// IJobDetail job = JobBuilder.Create<SyncPeer>()
//     .WithIdentity("SyncPeer", "group1")
//     .Build();
//
// ITrigger trigger = TriggerBuilder.Create()
//     .WithIdentity("SyncPeerTrigger",
//         "group1")
//     .StartNow()
//     .WithSimpleSchedule(x => x
//         .WithIntervalInSeconds(15)
//         .RepeatForever())
//     .Build();
//
// await scheduler.ScheduleJob(job, trigger);

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