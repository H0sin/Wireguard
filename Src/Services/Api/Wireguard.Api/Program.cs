using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Extensions;
using Wireguard.Api.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IInterfaceRepository, InterfaceRepository>();
builder.Services.AddScoped<IIpAddressRepository, IpAddressRepository>();

builder.Services.AddSingleton<ExceptionHandlerFilter>();

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