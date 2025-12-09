using System.Security.Claims;
using Common;
using Microsoft.EntityFrameworkCore;
using ProfileService.Data;
using ProfileService.DTOs;
using ProfileService.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddKeycloakAuthentication();
await builder.UserWolverineWithRabbitMqAsync(opts =>
{
    opts.ApplicationAssembly = typeof(Program).Assembly;
});
builder.AddNpgsqlDbContext<ProfileDbContext>("profileDb");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<UserProfileCreationMiddleware>();

app.MapGet("/profiles/me", async (ClaimsPrincipal user, ProfileDbContext db) =>
{
    var userId= user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
        return Results.Unauthorized();

    var profile = await db.UserProfiles.FindAsync(userId);
    return profile is null ? Results.NotFound() : Results.Ok(profile);

}).RequireAuthorization();

app.MapGet("/profiles/batch", async (string ids, ProfileDbContext db) =>
{
    var list = ids.Split(",", StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

    var rows = await db.UserProfiles
        .Where(x => list.Contains(x.Id))
        .Select(x => new ProfileSummaryDto(x.Id, x.DisplayName, x.Reputation))
        .ToListAsync();
    
    return Results.Ok(rows);
});

app.MapGet("/profiles", async (string? sortBy, ProfileDbContext db) =>
{
    var profilesQuery = db.UserProfiles.AsQueryable();
    switch (sortBy)
    {
        case "reputation":
            profilesQuery = profilesQuery.OrderByDescending(x => x.Reputation);
            break;
        default:
            profilesQuery = profilesQuery.OrderBy(x => x.DisplayName);
            break;
    }
    var profiles = await profilesQuery.ToListAsync();
    return Results.Ok(profiles);
});

app.MapGet("/profiles/{id}", async (string id, ProfileDbContext db) =>
{
    var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == id);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

app.MapPut("/profiles/edit", async (UpdateProfileDto dto, ProfileDbContext db, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();
    
    var profile = await db.UserProfiles.FindAsync(userId);
    if (profile is null) return Results.NotFound();
    
    profile.DisplayName = dto.DisplayName ?? profile.DisplayName;
    profile.Description = dto.Description ?? profile.Description;
    
    await db.SaveChangesAsync(); 
    return Results.NoContent();
}).RequireAuthorization();


using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<ProfileDbContext>();
    await context.Database.MigrateAsync(); 
}
catch (Exception e)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred seeding the DB.");
}



app.Run();

