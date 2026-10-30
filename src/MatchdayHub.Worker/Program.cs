using MatchdayHub.Infrastructure;
using MatchdayHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// The Worker is the scale-out story: the same ingestion pipeline as the API, but in a
// standalone process. It broadcasts over the SignalR Redis backplane so events still
// reach clients connected to the API. For the local single-process demo you run the
// API instead (Ingestion:RunInApi = true) and do not need this host.
builder.Services.AddMatchdayInfrastructure(builder.Configuration);
builder.Services.AddMatchdayRealtime(builder.Configuration);
builder.Services.AddMatchdayIngestionWorkers();

var host = builder.Build();

// Apply any pending migrations so the Worker can run against a fresh database too.
await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MatchdayDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
