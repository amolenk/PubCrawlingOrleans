using Microsoft.Azure.Cosmos;
using Orleans.Storage;

public sealed class ChangeFeedService : BackgroundService
{
    private readonly Container _monitorContainer;
    private readonly Container _leaseContainer;
    private readonly IClusterClient _clusterClient;
    private readonly IGrainStorageSerializer _serializer;

    private readonly ILogger _logger;

    public ChangeFeedService(
        IConfiguration configuration,
        IClusterClient clusterClient,
        IGrainStorageSerializer serializer,
        ILogger<ChangeFeedService> logger)
    {
        var cosmosClient = new CosmosClient(configuration["Cosmos:ConnectionString"]);

        _monitorContainer = cosmosClient.GetContainer(
            configuration["Cosmos:DatabaseName"],
            configuration["Cosmos:Containers:GrainState"]);

        _leaseContainer = cosmosClient.GetContainer(
            configuration["Cosmos:DatabaseName"],
            configuration["Cosmos:Containers:Leases"]);

        _clusterClient = clusterClient;
        _serializer = serializer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Change Feed Service");

        var processor = _monitorContainer
            .GetChangeFeedProcessorBuilder<GrainDocument>("SnapshotProcessor", HandleChangesAsync)
            .WithInstanceName(Guid.NewGuid().ToString())
            .WithLeaseContainer(_leaseContainer)
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            .WithErrorNotification(HandleErrorAsync)
            .WithLeaseAcquireNotification(HandleLeaseAcquiredAsync)
            .WithLeaseReleaseNotification(HandleLeaseReleasedAsync)
            .Build();

        await processor.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }

        await processor.StopAsync();
    }

    private async Task HandleChangesAsync(
        IReadOnlyCollection<GrainDocument> changes,
        CancellationToken cancellationToken)
    {
        if (changes.Any())
        {
            foreach (var change in changes)
            {
                // We're only interested in the Beer grains.
                if (change.GrainType == //.Id.StartsWith("beer_"))
                {
                    var beerGrainState = _serializer.Deserialize<BeerState>(
                        Convert.FromBase64String(change.State));

                    var eventId = GetEventIdFromBeerGrainId(change.GrainId);
                    var beerChartGrain = _clusterClient.GetGrain<IBeerChartGrain>(eventId);

                    await beerChartGrain.UpdateAsync(
                        beerGrainState.BeerId,
                        beerGrainState.LikedBy.Count,
                        beerGrainState.DislikedBy.Count)
                        .ConfigureAwait();
                }
            }
            
            _logger.LogInformation("Processing {ChangeCount} changes", changes.Count);
        }
    }

    private Task HandleErrorAsync(string leaseToken, Exception exception)
    {
        _logger.LogError(exception, "Error while processing change feed with lease {LeaseToken}", leaseToken);
        return Task.CompletedTask;
    }

    private Task HandleLeaseAcquiredAsync(string leaseToken)
    {
        _logger.LogInformation("Lease {LeaseToken} is acquired and will start processing", leaseToken);
        return Task.CompletedTask;
    }

    private Task HandleLeaseReleasedAsync(string leaseToken)
    {
        _logger.LogInformation("Lease {LeaseToken} is released and processing is stopped", leaseToken);
        return Task.CompletedTask;
    }

    private static Guid GetEventIdFromBeerGrainId(string grainId) =>  Guid.Parse(grainId.Split('/')[0]);
}