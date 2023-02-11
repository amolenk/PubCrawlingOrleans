using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Orleans;
using Orleans.Timers;
using Orleans.Utilities;
using Microsoft.Azure.Cosmos.Spatial;
using Orleans.Runtime;
using Microsoft.AspNetCore.SignalR;

// Split into read and write versions

interface IGeographyGrain : IGrainWithStringKey, IHandleVenueEvents
{
    //Task StartListeningAsync();

    Task AddLocation(Position position, string label);

    // Task RemoveLocation(string label);
}

public class GeographyGrain : Grain, IGeographyGrain, IRemindable
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<GeographyGrain> _logger;
    private readonly IHubContext<GeographyHub, IGeographyHub> _hubContext;

    public GeographyGrain(
        IConfiguration configuration,
        ILogger<GeographyGrain> logger,
        IHubContext<GeographyHub, IGeographyHub> hubContext)
    {
        _cosmosClient = new CosmosClient(configuration["CosmosDb:ConnectionString"]);
        _container = _cosmosClient.GetContainer(
            configuration["CosmosDb:DatabaseName"],
            configuration["CosmosDb:ContainerName"]
        );
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task AddLocation(Position position, string label)
    {
        await UpsertVenueAsync(position, label);

        // TODO If system restarts, we need to auto-start listening.
        await StartListeningAsync();
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        _logger.LogInformation("RECEIVED REMINDER");

        var query = _container.GetItemQueryIterator<VenuePosition>(
            new QueryDefinition("SELECT * FROM c WHERE c.eventId = @eventId")
                .WithParameter("@eventId", this.GetPrimaryKeyString())
        );

        // TODO Stop reminder when there are no results

        while (query.HasMoreResults)
        {
            var results = await query.ReadNextAsync();

            foreach (var result in results)
            {
                var venueGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(result.Label);
                await venueGrain.SubscribeAsync(this.AsReference<IHandleVenueEvents>());
            }
        }
    }

    public async Task StartListeningAsync()
    {
        _logger.LogInformation("STARTING LISTENING");

        await this.RegisterOrUpdateReminder(
            "EnsureSubscriptions",
            TimeSpan.FromSeconds(60), // TODO Check how initial due time works, doesn't seem to fire with zero or low values
            TimeSpan.FromSeconds(60));

        await ReceiveReminder("EnsureSubscriptions", default(TickStatus));
    }

    async Task IHandleVenueEvents.OnNumberOfCrawlersChangedAsync(int crawlerCount)
    {
        await _hubContext.Clients.All.ReceiveMessage("Orleans", $"OnNumberOfCrawlersChangedAsync: {crawlerCount}");

        _logger.LogInformation("UPDATE CRAWLER ACOUNT TO: " + crawlerCount);
    }

    private async Task UpsertVenueAsync(Position position, string label)
    {
        var venuePosition = new VenuePosition
        {
            Id = $"{this.GetPrimaryKeyString()}:{label}",
            EventId = this.GetPrimaryKeyString(),
            Label = label,
            Location = new Point(position.Longitude, position.Latitude)
        };

        await _container.UpsertItemAsync(venuePosition);
    }
}

public class VenuePosition
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("eventId")]
    public string EventId { get; set; } = null!;
    
    [JsonProperty("label")]
    public string Label { get; set; } = null!;

    [JsonProperty("location")]
    public Point Location { get; set; } = null!;
}