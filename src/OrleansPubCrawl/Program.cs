using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;
using Orleans.Runtime;
using Orleans.Hosting;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("memory");
    siloBuilder.UseInMemoryReminderService();
});

var app = builder.Build();

app.UseResponseCompression();

app.MapHub<GeographyHub>("/geohub");

// var cosmosClient = new CosmosClient(builder.Configuration["CosmosDb:ConnectionString"]);
// var container = cosmosClient.GetContainer(
//     builder.Configuration["CosmosDb:DatabaseName"],
//     builder.Configuration["CosmosDb:ContainerName"]);

// await container.CreateItemAsync(new VenuePosition
//     {
//         EventId = "pubcrawl",
//         Label = "cosmosdb",
//         Location = new Point (-122.12, 47.66)
//     });

// return;


app.MapPost("/events/{eventId}/venues/{venueId}",
    async (IGrainFactory grainFactory, string eventId, string venueId) =>
    {
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
        
        var eventGrain = grainFactory.GetGrain<IEventGrain>(eventId);
        await eventGrain.RegisterVenueAsync(venueGrain);

        return Results.Ok();
    });

app.MapPost("/events/{eventId}/venues/{venueId}/crawlers/{crawlerId}",
    async (IGrainFactory grainFactory, string eventId, string venueId, string crawlerId) =>
    {    
        var eventGrain = grainFactory.GetGrain<IEventGrain>(eventId);
        if (await eventGrain.IsRegisteredVenueAsync(venueId))
        {
            var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
            var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);

            await venueGrain.CheckInAsync(crawlerGrain);

            return Results.Ok();
        }

        return Results.BadRequest($"Venue {venueId} does not take part in this event.");
    });

app.MapDelete("/events/{eventId}/venues/{venueId}/crawlers/{crawlerId}",
    async (IGrainFactory grainFactory, string eventId, string venueId, string crawlerId) =>
    {
        var eventGrain = grainFactory.GetGrain<IEventGrain>(eventId);
        if (await eventGrain.IsRegisteredVenueAsync(venueId))
        {
            var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
            var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);

            await venueGrain.CheckOutAsync(crawlerGrain);
        }

        return Results.Ok();
    });

app.Run();
