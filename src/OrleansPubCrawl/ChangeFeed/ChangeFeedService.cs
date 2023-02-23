// using Microsoft.Azure.Cosmos;
// using Orleans.Runtime;
// using Orleans.Storage;

// // TODO Turn this into a cluster/silo service
// public sealed class ChangeFeedService : BackgroundService
// {
//     private readonly Container _monitorContainer;
//     private readonly Container _leaseContainer;
//     private readonly IClusterClient _clusterClient;
//     private readonly IGrainStorageSerializer _serializer;

//     private readonly ILogger _logger;

//     public ChangeFeedService(
//         IConfiguration configuration,
//         IClusterClient clusterClient,
//         IGrainStorageSerializer serializer,
//         ILogger<ChangeFeedService> logger)
//     {
//         var cosmosClient = new CosmosClient(configuration["Cosmos:ConnectionString"]);

//         _monitorContainer = cosmosClient.GetContainer(
//             configuration["Cosmos:DatabaseName"],
//             configuration["Cosmos:Containers:GrainState"]);

//         _leaseContainer = cosmosClient.GetContainer(
//             configuration["Cosmos:DatabaseName"],
//             configuration["Cosmos:Containers:Leases"]);

//         _clusterClient = clusterClient;
//         _serializer = serializer;
//         _logger = logger;
//     }

//     protected override async Task ExecuteAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Starting Change Feed Service");

//         var processor = _monitorContainer
//             .GetChangeFeedProcessorBuilder<GrainDocument>("SnapshotProcessor", HandleChangesAsync)
//             .WithInstanceName(Guid.NewGuid().ToString())
//             .WithLeaseContainer(_leaseContainer)
//             .WithStartTime(DateTime.MinValue.ToUniversalTime())
//             .WithErrorNotification(HandleErrorAsync)
//             .WithLeaseAcquireNotification(HandleLeaseAcquiredAsync)
//             .WithLeaseReleaseNotification(HandleLeaseReleasedAsync)
//             .Build();

//         await processor.StartAsync();

//         try
//         {
//             await Task.Delay(Timeout.Infinite, cancellationToken);
//         }
//         catch (TaskCanceledException)
//         {
//             // Ignore
//         }

//         await processor.StopAsync();
//     }

//     private async Task HandleChangesAsync(
//         IReadOnlyCollection<GrainDocument> changes,
//         CancellationToken cancellationToken)
//     {
//         // Try to keep any business logic outside of this infrastructure code.

//         if (changes.Any())
//         {
//             foreach (var change in changes)
//             {
//                 if (GrainId.TryParse(change.GrainId, out var grainId))
//                 {
//                     var changeFeedGrain = _clusterClient.GetGrain<IChangeFeedGrain>(0);
//                     await changeFeedGrain.ProcessAsync(grainId, change.State);
//                 }


//                 // var grainId = GrainId.FromKeyString(change.Id);


//                 // var (grainType, eventId, keyExt) = ParseDocumentId(change.Id);
//                 // Console.WriteLine(grainType);
//                 // Console.WriteLine(eventId);

//                 // // We're only interested in the Beer grains.
//                 // if (grainType == "beerrating")
//                 // {

//                 //     var beerTotalScoreGrain = _clusterClient.GetGrain<IBeerRatingGrain>(
//                 //             eventId,
//                 //             keyExt,
//                 //             null);


//                 //     // Making a call to a stateless worker grain is the same as to any other grain. The only difference
//                 //     // is that in most cases a single grain ID is used, 0 or Guid.Empty. Multiple grain IDs can be used
//                 //     // when having multiple stateless worker grain pools, one per ID is desirable.
//                 //     _clusterClient.GetGrain<IBeerScoreAggregatorGrain>(0)
//                 //         .AggregateAsync( eventId, compoundKey);


//                 //     var beerRatingState = _serializer.Deserialize<BeerRatingState>(
//                 //         Convert.FromBase64String(change.State));

//                 //     foreach (var (beerId, rating) in beerRatingState.Ratings)
//                 //     {
//                 //         var beerTotalScoreGrain = _clusterClient.GetGrain<IBeerTotalScoreGrain>(
//                 //             eventId,
//                 //             beerId,
//                 //             null);

//                 //         _logger.LogInformation(
//                 //             "Updating beer {BeerId} with rating {Rating} for event {EventId}",
//                 //             beerId,
//                 //             rating,
//                 //             eventId);

//                 //         await beerTotalScoreGrain.UpdateScoreAsync(compoundKey, rating)
//                 //             .ConfigureAwait(false);
//                 //     }
//                 // }
//                 // else if (grainType == "beertotalscore")
//                 // {
//                 //     var beerTotalScoreState = _serializer.Deserialize<BeerRatingState>(
//                 //         Convert.FromBase64String(change.State));

//                 //     var totalScore = beerTotalScoreState.Ratings.Values.Sum();

//                 //     foreach (var (beerId, rating) in beerRatingState.Ratings)
//                 //     {
//                 //         var beerTotalScoreGrain = _clusterClient.GetGrain<IBeerTotalScoreGrain>(
//                 //             eventId,
//                 //             beerId,
//                 //             null);

//                 //         _logger.LogInformation(
//                 //             "Updating beer {BeerId} with rating {Rating} for event {EventId}",
//                 //             beerId,
//                 //             rating,
//                 //             eventId);

//                 //         await beerTotalScoreGrain.UpdateScoreAsync(compoundKey, rating)
//                 //             .ConfigureAwait(false);
//                 //     }
//                 // }
//             }
            
//             _logger.LogInformation("Processed {ChangeCount} changes", changes.Count);
//         }
//     }

//     private Task HandleErrorAsync(string leaseToken, Exception exception)
//     {
//         _logger.LogError(exception, "Error while processing change feed with lease {LeaseToken}", leaseToken);
//         return Task.CompletedTask;
//     }

//     private Task HandleLeaseAcquiredAsync(string leaseToken)
//     {
//         _logger.LogInformation("Lease {LeaseToken} is acquired and will start processing", leaseToken);
//         return Task.CompletedTask;
//     }

//     private Task HandleLeaseReleasedAsync(string leaseToken)
//     {
//         _logger.LogInformation("Lease {LeaseToken} is released and processing is stopped", leaseToken);
//         return Task.CompletedTask;
//     }

//     private static Guid GetEventIdFromBeerGrainId(string grainId) =>  Guid.Parse(grainId.Split('/')[0]);

//     // TODO There must be a better name for compoundKey
//     private static (string GrainType, long EventId, string KeyExt) ParseDocumentId(string documentId)
//     {
//         var parts = documentId.Split('_', '+');

//         return (parts[0], long.Parse(parts[1]), parts.Length > 2 ? parts[2] : string.Empty);
//     }
// }