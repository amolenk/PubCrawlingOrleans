using Microsoft.AspNetCore.Mvc;
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

    // siloBuilder.AddAzureCosmosGrainStorage("memory", options =>
    // {
    //     options.ConfigureCosmosClient(
    //         builder.Configuration["Cosmos:ConnectionString"]!,
    //         builder.Configuration["Cosmos:DatabaseName"]!,
    //         builder.Configuration["Cosmos:Containers:GrainState"]!);
    // });

    // siloBuilder.AddAzureCosmosGrainStorageAsDefault(options =>
    // {
    //     options.ConfigureCosmosClient(
    //         builder.Configuration["Cosmos:ConnectionString"]!,
    //         builder.Configuration["Cosmos:DatabaseName"]!,
    //         builder.Configuration["Cosmos:Containers:GrainState"]!);
    // });

    siloBuilder.AddMemoryGrainStorage("memory");
    siloBuilder.AddMemoryGrainStorageAsDefault();
    siloBuilder.UseInMemoryReminderService();

    // siloBuilder.AddGrainService<AzureCosmosChangeFeedService>();
});

var app = builder.Build();

app.UseResponseCompression();

app.MapHub<GeographyHub>("/geohub");

// Register an event.
app.MapPost("/events/{eventId}",
    async (IGrainFactory grainFactory, int eventId, [FromBody] EventRegistration registration) =>
    {
        // TODO Validate that the registration is valid.

        var beerSelectionGrain = grainFactory.GetGrain<IBeerSelectionGrain>(eventId);
        await beerSelectionGrain.AddOrUpdateBeersAsync(registration.Beers);

        var eventMapGrain = grainFactory.GetGrain<IEventMapGrain>(eventId);

        foreach (var venue in registration.Venues)
        {
            var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venue.Id, null);
            await venueGrain.RegisterAsync(venue);

            await eventMapGrain.AddOrUpdateVenueLocationAsync(venue);
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
app.MapGet("/events/{eventId}/beers/highscores",
    async (IGrainFactory grainFactory, int eventId) =>
    {
        var beerHighScoresGrain = grainFactory.GetGrain<IBeerHighScoresGrain>(eventId);
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
        var result = await venueGrain.GetAsync();

        return Results.Ok(result);
    });

// Check-in a crawler at a venue.
app.MapPost("/events/{eventId}/venues/{venueId}/crawlers/{crawlerId}",
    async (IGrainFactory grainFactory, int eventId, string venueId, string crawlerId) =>
    {
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId);

        var result = await venueGrain.TryCheckInAsync(crawlerId);
        if (!result)
        {
            return Results.BadRequest($"Venue {venueId} does not take part in this event.");
        }

        return Results.Ok();
    });

// Check-out a crawler from a venue.
app.MapDelete("/events/{eventId}/venues/{venueId}/crawlers/{crawlerId}",
    async (IGrainFactory grainFactory, int eventId, string venueId, string crawlerId) =>
    {
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId);
        await venueGrain.CheckOutAsync(crawlerId);

        return Results.Ok();
    });

// Rate a beer
app.MapPut("/events/{eventId}/ratings/{crawlerId}/beers/{beerId}",
    async (IGrainFactory grainFactory, int eventId, string crawlerId, string beerId, [FromBody] int rating) =>
    {
        var beerRatingGrain = grainFactory.GetGrain<IBeerRatingGrain>(eventId, crawlerId);
        var result = await beerRatingGrain.TryRateAsync(beerId, rating);
    
        if (!result)
        {
            return Results.BadRequest("Beer is not available at this event.");
        }

        return Results.Ok();
    });

// Get beer ratings
app.MapGet("/events/{eventId}/ratings/{crawlerId}",
    async (IGrainFactory grainFactory, int eventId, string crawlerId) =>
    {
        var beerRatingGrain = grainFactory.GetGrain<IBeerRatingGrain>(eventId, crawlerId);
        var result = await beerRatingGrain.GetAllAsync();
    
        return Results.Ok(result);
    });

app.Run();
