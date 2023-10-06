using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

var instanceId = builder.Configuration.GetValue<int>("InstanceId", 0);

builder.Host.UseOrleans(siloBuilder =>
{
    if (instanceId == 0)
    {
        siloBuilder.UseDashboard(options => { });
    }

    siloBuilder.UseLocalhostClustering(
        siloPort: 11111 + instanceId,
        gatewayPort: 30000 + instanceId,
        primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111));

    siloBuilder.AddMemoryGrainStorageAsDefault();    
});

builder.Services.AddHostedService<EventMapObserverService>();

var app = builder.Build();

app.UseResponseCompression();

app.MapHub<EventMapHub>("/eventmaphub");

// Register an event.
app.MapPost("/events/{eventId}",
    async (IGrainFactory grainFactory, int eventId, [FromBody] EventRegistration registration) =>
    {
        var beerSelectionGrain = grainFactory.GetGrain<IBeerSelectionGrain>(eventId);
        await beerSelectionGrain.AddOrUpdateBeersAsync(registration.Beers);

        foreach (var venue in registration.Venues)
        {
            var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(primaryKey: eventId, keyExtension: venue.Id);
            await venueGrain.RegisterAsync(venue);
        }

        return Results.Ok();
    });

// Get the beer selection for an event.
app.MapGet("/events/{eventId}/beers",
    async (IGrainFactory grainFactory, int eventId) =>
    {
        var beerSelectionGrain = grainFactory.GetGrain<IBeerSelectionGrain>(eventId);
        var result = await beerSelectionGrain.GetAllAsync();

        return Results.Ok(result);
    });

// Get the beer high-score list.
app.MapGet("/events/{eventId}/beers/leaderboard", 
    async (IGrainFactory grainFactory, int eventId) =>
    {
        var beerHighScoresGrain = grainFactory.GetGrain<IBeerLeaderboardGrain>(eventId);
        var result = await beerHighScoresGrain.GetTopBeersAsync();

        return Results.Ok(result);
    });

// Get the list of venues.
app.MapGet("/events/{eventId}/venues",
    async (IGrainFactory grainFactory, int eventId) =>
    {
        var eventMapGrain = grainFactory.GetGrain<IEventMapGrain>(eventId);
        var result = await eventMapGrain.GetAsync();

        return Results.Ok(result);
    });

// Get the information for a specific venue.
app.MapGet("/events/{eventId}/venues/{venueId}",
    async (IGrainFactory grainFactory, int eventId, string venueId) =>
    {
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId);
        var result = await venueGrain.GetDetailsAsync();

        return Results.Ok(result);
    });

// Check-in a crawler at a venue.
app.MapPost("/events/{eventId}/venues/{venueId}/crawlers",
    async (IGrainFactory grainFactory, int eventId, string venueId, [FromHeader] string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(primaryKey: eventId, keyExtension: crawlerId);
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(primaryKey: eventId, keyExtension: venueId);

        try
        {
            await crawlerGrain.CheckInAsync(venueGrain);
        }
        catch (VenueNotAvailableException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        return Results.Ok();
    });

// Get crawler status
app.MapGet("/events/{eventId}/crawlers/status",
    async (IGrainFactory grainFactory, int eventId, [FromHeader] string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(primaryKey: eventId, keyExtension: crawlerId);
        var result = await crawlerGrain.GetStatusAsync();
    
        return Results.Ok(result);
    });

// Check-out a crawler from a venue.
app.MapDelete("/events/{eventId}/crawlers/status/checkin", 
    async (IGrainFactory grainFactory, int eventId, [FromHeader] string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(primaryKey: eventId, keyExtension: crawlerId);
        await crawlerGrain.CheckOutAsync();

        return Results.Ok();
    });

// Rate a beer
app.MapPut("/events/{eventId}/beers/{beerId}/ratings",
    async (IGrainFactory grainFactory, int eventId, string beerId, [FromHeader] string crawlerId, [FromBody] int rating) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(primaryKey: eventId, keyExtension: crawlerId);
        
        try
        {
            await crawlerGrain.RateBeerAsync(beerId, rating);
        }
        catch (BeerNotAvailableException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        return Results.Ok();
    });

app.Run($"https://+:{5001 + instanceId}");
