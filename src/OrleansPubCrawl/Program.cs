using Microsoft.AspNetCore.Http.Extensions;
using Orleans.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("venue");
});

var app = builder.Build();

app.MapPost("/venues/{venueId}/checkin/{crawlerId}",
    async (IGrainFactory grainFactory, string venueId, string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);
        
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
        await venueGrain.CheckInAsync(crawlerGrain);

        return Results.Ok();
    });

app.MapPost("/venues/{venueId}/checkout/{crawlerId}",
    async (IGrainFactory grainFactory, string venueId, string crawlerId) =>
    {
        var crawlerGrain = grainFactory.GetGrain<ICrawlerGrain>(crawlerId);
        
        var venueGrain = grainFactory.GetGrain<IDrinkingVenueGrain>(venueId);
        await venueGrain.CheckOutAsync(crawlerGrain);

        return Results.Ok();
    });

app.Run();
