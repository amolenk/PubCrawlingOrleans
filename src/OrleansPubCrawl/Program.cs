using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;
using Orleans.Runtime;
using Orleans.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddHostedService<ChangeFeedService>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    siloBuilder.AddAzureCosmosGrainStorage("memory", options =>
    {
        options.ConfigureCosmosClient(
            builder.Configuration["Cosmos:ConnectionString"]!,
            builder.Configuration["Cosmos:DatabaseName"]!,
            builder.Configuration["Cosmos:Containers:GrainState"]!);
    });

    siloBuilder.AddAzureCosmosGrainStorageAsDefault(options =>
    {
        options.ConfigureCosmosClient(
            builder.Configuration["Cosmos:ConnectionString"]!,
            builder.Configuration["Cosmos:DatabaseName"]!,
            builder.Configuration["Cosmos:Containers:GrainState"]!);
    });

    // siloBuilder.AddMemoryGrainStorage("memory");
    // siloBuilder.AddMemoryGrainStorageAsDefault();
    siloBuilder.UseInMemoryReminderService();
});

var app = builder.Build();

app.UseResponseCompression();

app.MapHub<GeographyHub>("/geohub");

// Get the list of drinking venues including attendance.
// app.MapGet("/events/{eventId}/attendance",
//     async (IGrainFactory grainFactory, Guid eventId) =>
//     {
//         var mapGrain = grainFactory.GetGrain<IEventMapGrain>(eventId);

//         return Results.Ok(await mapGrain.GetAttendanceAsync());
//     });

app.MapPost("/events/{eventId}/venues/{venueId}",
    async (IGrainFactory grainFactory, Guid eventId, string venueId) =>
    {
        var eventGrain = grainFactory.GetGrain<IEventGrain>(eventId);
        await eventGrain.RegisterVenueAsync(venueId);

        return Results.Ok();
    });

// app.MapPost("/events/{eventId}/crawlers/{crawlerId}",
//     async (IGrainFactory grainFactory, string eventId, string crawlerId) =>
//     {
//         var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);
//         await crawlerGrain.JoinEventAsync(eventId);

//         return Results.Ok();
//     });


app.MapPost("/events/{eventId}/venues/{venueId}/crawlers/{crawlerId}",
    async (IGrainFactory grainFactory, Guid eventId, string venueId, string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);
        await crawlerGrain.CheckInAsync(eventId, venueId); 

        // var eventGrain = grainFactory.GetGrain<IEventGrain>(eventId);
        // if (await eventGrain.IsRegisteredVenueAsync(venueId))
        // {
        //     var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
        //     var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);

        //     await venueGrain.CheckInAsync(crawlerGrain);

        //     return Results.Ok();
        // }

        // return Results.BadRequest($"Venue {venueId} does not take part in this event.");
    });

// Register beer
app.MapPost("/events/{eventId}/venues/{venueId}/beers/{beerId}",
    async (IGrainFactory grainFactory, Guid eventId, string venueId, string beerId) =>
    {
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId, null);
        await venueGrain.RegisterBeerAsync(beerId);

        return Results.Ok();
    });


// Like beer
app.MapPost("/events/{eventId}/venues/{venueId}/beers/{beerId}/likes",
    async (IGrainFactory grainFactory, Guid eventId, string venueId, string beerId, [FromQuery] string crawlerId) =>
    {
        var beerKey = BeerGrain.GetKey(eventId, venueId, beerId);

        var beerGrain = grainFactory.GetGrain<IBeerGrain>(beerKey);
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);

        await beerGrain.LikeAsync(crawlerGrain);

        return Results.Ok();
    });

// Dislike beer
app.MapPost("/events/{eventId}/venues/{venueId}/beers/{beerId}/dislikes",
    async (IGrainFactory grainFactory, Guid eventId, string venueId, string beerId, [FromQuery] string crawlerId) =>
    {
        var beerKey = BeerGrain.GetKey(eventId, venueId, beerId);

        var beerGrain = grainFactory.GetGrain<IBeerGrain>(beerKey);
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);

        await beerGrain.DislikeAsync(crawlerGrain);

        return Results.Ok();
    });


// app.MapDelete("/events/{eventId}/venues/{venueId}/crawlers/{crawlerId}",
//     async (IGrainFactory grainFactory, string eventId, string venueId, string crawlerId) =>
//     {
//         var eventGrain = grainFactory.GetGrain<IEventGrain>(eventId);
//         if (await eventGrain.IsRegisteredVenueAsync(venueId))
//         {
//             var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
//             var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);

//             await venueGrain.CheckOutAsync(crawlerGrain);
//         }

//         return Results.Ok();
//     });

app.Run();
