using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddHostedService<EventMapHubListUpdater>();

builder.Host.UseOrleans(siloBuilder =>
{
    // siloBuilder.ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory());
    siloBuilder.UseDashboard(options => { });

    siloBuilder.UseLocalhostClustering();

    siloBuilder.AddMemoryGrainStorageAsDefault();
    // siloBuilder.AddAzureTableGrainStorageAsDefault(options =>
    // {
    //     options.ConfigureTableServiceClient(builder.Configuration["Storage:ConnectionString"]!);
    // });    

    // siloBuilder.AddAzureCosmosGrainStorage("cosmos", options =>
    // {
    //     options.ConfigureCosmosClient(
    //         builder.Configuration["Cosmos:ConnectionString"]!,
    //         builder.Configuration["Cosmos:DatabaseName"]!,
    //         builder.Configuration["Cosmos:Containers:GrainState"]!);
    // });
    siloBuilder.UseInMemoryReminderService();
});


// builder.Host.UseOrleans((ctx, siloBuilder) => {

//     // In order to support multiple hosts forming a cluster, they must listen on different ports.
//     // Use the --InstanceId X option to launch subsequent hosts.
//     int instanceId = ctx.Configuration.GetValue<int>("InstanceId");
//     siloBuilder.UseLocalhostClustering(
//         siloPort: 11111 + instanceId,
//         gatewayPort: 30000 + instanceId,
//         primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111));

//     siloBuilder.AddActivityPropagation();
// });
// builder.WebHost.UseKestrel((ctx, kestrelOptions) =>
// {
//     // To avoid port conflicts, each Web server must listen on a different port.
//     int instanceId = ctx.Configuration.GetValue<int>("InstanceId");
//     kestrelOptions.ListenLocalhost(5001 + instanceId);
// });
// builder.Services.AddHostedService<HubListUpdater>();

var app = builder.Build();

app.UseResponseCompression();

// TODO Change path
app.MapHub<EventMapHub>("/geohub");

// Register an event.
app.MapPost("/events/{eventId}",
    async (IGrainFactory grainFactory, int eventId, [FromBody] EventRegistration registration) =>
    {
        // TODO Validate that the registration is valid.

        var beerSelectionGrain = grainFactory.GetGrain<IBeerSelectionGrain>(eventId);
        await beerSelectionGrain.AddOrUpdateBeersAsync(registration.Beers); // TODO SetBeersAsync

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
        var result = await venueGrain.GetSummaryAsync();

        return Results.Ok(result);
    });

// Check-in a crawler at a venue.
app.MapPost("/events/{eventId}/venues/{venueId}/crawlers",
    async (IGrainFactory grainFactory, int eventId, string venueId, [FromHeader] string crawlerId) =>
    {
        try
        {
            var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
            await crawlerGrain.CheckInAsync(venueId);
        }
        catch (VenueNotAvailableException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        // var currentVenueId = await crawlerGrain.GetVenueAsync();

        // if (currentVenueId.Length > 0 && currentVenueId != venueId)
        // {
        //     // Crawler is already checked in somewhere else.
        //     // Let's check them out from there.
        //     var currentVenueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, currentVenueId, null);
        //     await currentVenueGrain.CheckOutAsync(crawlerId);
        // }

        // var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId);

        // try
        // {
        //     await venueGrain.CheckInAsync(crawlerId);
        // }
        // catch (VenueNotAvailableException ex)
        // {
        //     return Results.BadRequest(ex.Message);
        // }

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
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId);
        
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

// Get beer ratings
app.MapGet("/events/{eventId}/crawlers/status",
    async (IGrainFactory grainFactory, int eventId, [FromHeader] string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId);
        var result = await crawlerGrain.GetStatusAsync();
    
        return Results.Ok(result);
    });

app.Run();
