// using Orleans.Concurrency;

// public interface IBeerScoreAggregatorGrain
// {
//     Task AggregateRatingsAsync(IBeerRatingGrain beerRatingGrain);
// }

// [StatelessWorker]
// public class BeerScoreAggregatorGrain : Grain, IBeerScoreAggregatorGrain
// {
//     private readonly ILogger _logger;

//     public BeerScoreAggregatorGrain(ILogger<BeerScoreAggregatorGrain> logger)
//     {
//         _logger = logger;
//     }

//     public async Task AggregateRatingsAsync(IBeerRatingGrain beerRatingGrain)
//     {
//         var eventId = beerRatingGrain.GetPrimaryKeyLong();

//         var beerTotalScoreGrain = GrainFactory.GetGrain<IBeerTotalScoreGrain>(eventId, beerId, null);
        
//         foreach (var (crawlerId, rating) in await beerRatingGrain.GetAllAsync())
//         {
//             var result = await beerTotalScoreGrain.UpdateScoreAsync(crawlerId, rating);

//             _logger.LogInformation(
//                 "Updated beer {BeerId} with rating {Rating} for event {EventId} with result {Result}",
//                 beerId,
//                 rating,
//                 eventId,
//                 result);
//         }
//     }
// }