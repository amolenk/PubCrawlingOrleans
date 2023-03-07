// using Microsoft.Azure.Cosmos;
// using Orleans.Runtime;
// using Orleans.Services;

// public interface IAzureCosmosChangeFeedService : IGrainService
// {
// }

// public class AzureCosmosChangeFeedService : GrainService, IAzureCosmosChangeFeedService
// {
//     private readonly IGrainFactory _grainFactory;
//     private readonly IConfiguration _configuration;
//     private readonly ILogger _logger;
//     private ChangeFeedProcessor _processor = null!;

//     public AzureCosmosChangeFeedService(
//         GrainId id,
//         Silo silo,
//         IGrainFactory grainFactory,
//         IConfiguration configuration,
//         ILoggerFactory loggerFactory)
//         : base(id, silo, loggerFactory)
//     {
//         _grainFactory = grainFactory;
//         _configuration = configuration;
//         _logger = loggerFactory.CreateLogger<AzureCosmosChangeFeedService>();
//     }

//     public override Task Init(IServiceProvider serviceProvider)
//     {
//         _logger.LogInformation("Initializing Change Feed Service");

//         var cosmosClient = new CosmosClient(_configuration["Cosmos:ConnectionString"]);

//         var monitorContainer = cosmosClient.GetContainer(
//             _configuration["Cosmos:DatabaseName"],
//             _configuration["Cosmos:Containers:GrainState"]);

//         var leaseContainer = cosmosClient.GetContainer(
//             _configuration["Cosmos:DatabaseName"],
//             _configuration["Cosmos:Containers:Leases"]);


//         _processor = monitorContainer
//             .GetChangeFeedProcessorBuilder<GrainDocument>("SnapshotProcessor", HandleChangesAsync)
//             .WithInstanceName(Guid.NewGuid().ToString())
//             .WithLeaseContainer(leaseContainer)
//             .WithStartTime(DateTime.MinValue.ToUniversalTime())
//             .WithErrorNotification(HandleErrorAsync)
//             .WithLeaseAcquireNotification(HandleLeaseAcquiredAsync)
//             .WithLeaseReleaseNotification(HandleLeaseReleasedAsync)
//             .Build();

//         return base.Init(serviceProvider);
//     }

//     public override Task Start() => _processor.StartAsync();

//     public override Task Stop() => _processor.StopAsync();

//     private async Task HandleChangesAsync(
//         IReadOnlyCollection<GrainDocument> changes,
//         CancellationToken cancellationToken)
//     {
//         // Try to keep any business logic outside of this infrastructure code.

//         if (changes.Any())
//         {
//             var processChangeFeedGrain = _grainFactory.GetGrain<IProcessChangeFeedGrain>(0);

//             // TODO Parallelize this
//             foreach (var change in changes)
//             {
//                 if (GrainId.TryParse(change.GrainId, out var grainId))
//                 {
//                     await processChangeFeedGrain.ProcessAsync(grainId, change.State);
//                 }
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
// }