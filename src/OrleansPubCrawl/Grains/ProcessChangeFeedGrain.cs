using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Storage;

public interface IProcessChangeFeedGrain : IGrainWithIntegerKey
{
    Task ProcessAsync(GrainId grainId, string grainState);
}

[StatelessWorker]
public class ProcessChangeFeedGrain : Grain, IProcessChangeFeedGrain
{
    private static readonly GrainType BeerRatingGrainType = GrainType.Create("beerrating");
    private static readonly GrainType BeerTotalScoreGrainType = GrainType.Create("beertotalscore");

    private readonly IGrainStorageSerializer _serializer;
    private readonly ILogger _logger;

    public ProcessChangeFeedGrain(IGrainStorageSerializer serializer, ILogger<ProcessChangeFeedGrain> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    public async Task ProcessAsync(GrainId grainId, string grainState)
    {
        if (grainId.Type == BeerRatingGrainType)
        {
            var eventId = grainId.GetIntegerKey(out var crawlerId);

            // var beerRatingState = _serializer.Deserialize<BeerRatingState>(
            //     Convert.FromBase64String(grainState));

            var beerRatingGrain = GrainFactory.GetGrain<IBeerRatingGrain>(grainId);
            var ratings = await beerRatingGrain.GetAllAsync();

            foreach ((string beerId, int rating) in ratings)
            {
                var beerTotalScoreGrain = GrainFactory.GetGrain<IBeerTotalScoreGrain>(eventId, beerId, null);

                await beerTotalScoreGrain.UpdateRatingAsync(crawlerId!, rating);
            }
        }
        // Alternatively, set a reminder in the BeerTotalScoreGrain to update the BeerHighScoresGrain every x minutes.
        else if (grainId.Type == BeerTotalScoreGrainType)
        {
            var eventId = grainId.GetIntegerKey(out var beerId);

            var beerTotalScoreGrain = GrainFactory.GetGrain<IBeerTotalScoreGrain>(grainId);
            var beerHighScoresGrain = GrainFactory.GetGrain<IBeerHighScoresGrain>(eventId);

            var score = await beerTotalScoreGrain.GetTotalScoreAsync();         
            
            await beerHighScoresGrain.UpdateScoreAsync(beerId!, score);
        }
    }
}