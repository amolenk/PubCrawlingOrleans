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

// TODO Create GrainService so that it starts after the silo is up and running.
builder.Services.AddHostedService<EventMapHubListUpdater>();

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

    // siloBuilder.AddAzureTableGrainStorageAsDefault(options =>
    // {
    //     options.ConfigureTableServiceClient(builder.Configuration["Storage:ConnectionString"]!);
    // });    
});

var app = builder.Build();

app.UseResponseCompression();

// TODO Change path
app.MapHub<EventMapHub>("/geohub");

// Register an event.
app.MapPost("/events/{eventId}",
    async (IGrainFactory grainFactory, int eventId, [FromBody] EventRegistration registration) =>
    {
        var beerSelectionGrain = grainFactory.GetGrain<IBeerSelectionGrain>(eventId);
        await beerSelectionGrain.AddOrUpdateBeersAsync(registration.Beers); // TODO SetBeersAsync

//        var eventMapGrain = grainFactory.GetGrain<IEventMapGrain>(eventId);

        foreach (var venue in registration.Venues)
        {
            var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venue.Id, null);
            await venueGrain.RegisterAsync(venue);

  //          await eventMapGrain.AddOrUpdateVenueLocationAsync(venue);
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
app.MapGet("/events/{eventId}/beers/highscores", // TODO Leaderboard
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
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId, null);
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);

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

// Check-out a crawler from a venue.
app.MapDelete("/events/{eventId}/venues/{venueId}/crawlers", // TODO Better path
    async (IGrainFactory grainFactory, int eventId, string venueId, [FromHeader] string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
        await crawlerGrain.CheckOutAsync();

        // var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId);
        // await venueGrain.CheckOutAsync(crawlerId);

        return Results.Ok();
    });

// Rate a beer
app.MapPut("/events/{eventId}/beers/{beerId}/ratings",
    async (IGrainFactory grainFactory, int eventId, string beerId, [FromHeader] string crawlerId, [FromBody] int rating) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
        
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

// Get crawler status
app.MapGet("/events/{eventId}/crawlers/status",
    async (IGrainFactory grainFactory, int eventId, [FromHeader] string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
        var result = await crawlerGrain.GetStatusAsync();
    
        return Results.Ok(result);
    });

app.Run($"https://+:{5001 + instanceId}");
