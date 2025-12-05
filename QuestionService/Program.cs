using System.Net.Sockets;
using Common;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using QuestionService.Data;
using QuestionService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults(); // Setups health checks and telemetry stuff

builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();
builder.Services.AddKeycloakAuthentication();

builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");

// Start when messaging service is ready to receive and publish messages, not when the container has started
// Configuration for RabbitMQ client for this microservice
// Replaced the code so we don't copy-paste it in each project
await builder.UserWolverineWithRabbitMqAsync(opts =>
{
    opts.PublishAllMessages().ToRabbitExchange("questions");
    // Since Wolverine is not configured in this project, this line of code
    // tells it to look in this projects assembly code
    opts.ApplicationAssembly = typeof(Program).Assembly;
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
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<QuestionDbContext>();
    await context.Database.MigrateAsync(); // Create and apply migrations
}
catch (Exception e)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred seeding the DB.");
}

app.Run();