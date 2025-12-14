using System.Net.Sockets;
using Common;
using Contracts;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using QuestionService.Data;
using QuestionService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults(); // Setups health checks and telemetry stuff

builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();
builder.Services.AddKeycloakAuthentication();

var connectionString = builder.Configuration.GetConnectionString("questionDb");

builder.Services.AddDbContext<QuestionDbContext>(options =>
{
    options.UseNpgsql(connectionString);
}, optionsLifetime: ServiceLifetime.Singleton);

// Start when messaging service is ready to receive and publish messages, not when the container has started
// Configuration for RabbitMQ client for this microservice
// Replaced the code so we don't copy-paste it in each project
await builder.UserWolverineWithRabbitMqAsync(opts =>
{
    // Since Wolverine is not configured in this project, this line of code
    // tells it to look in this projects assembly code
    opts.ApplicationAssembly = typeof(Program).Assembly;
    opts.PersistMessagesWithPostgresql(connectionString!);
    opts.UseEntityFrameworkCoreTransactions();
    opts.PublishMessage<QuestionCreated>().ToRabbitExchange("Contracts.QuestionCreated").UseDurableOutbox();
    opts.PublishMessage<QuestionUpdated>().ToRabbitExchange("Contracts.QuestionUpdated").UseDurableOutbox();
    opts.PublishMessage<QuestionDeleted>().ToRabbitExchange("Contracts.QuestionDeleted").UseDurableOutbox();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

// Get hold of DbContext and apply non-applied Migrations
await app.MigrateDbContextAsync<QuestionDbContext>();

app.Run();