using Orleans.Runtime;
using Orleans.Utilities;

// TODO BeerRatingsGrain
public interface IBeerRatingGrain : IGrainWithIntegerCompoundKey
{
    Task<IDictionary<string, int>> GetAllAsync();

    Task<bool> TryRateAsync(string beerId, int rating);
}

public class BeerRatingGrain : Grain, IBeerRatingGrain
{
    private readonly IPersistentState<BeerRatingState> _state;
    private readonly ILogger _logger;

    public BeerRatingGrain(
        [PersistentState("state")] IPersistentState<BeerRatingState> state,
        ILogger<BeerRatingGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public Task<IDictionary<string, int>> GetAllAsync()
    {
        return Task.FromResult<IDictionary<string, int>>(_state.State.Ratings);
    }

    public async Task<bool> TryRateAsync(string beerId, int rating)
    {
        var beerSelectionGrain = GrainFactory.GetGrain<IBeerSelectionGrain>(EventId);
        if (!await beerSelectionGrain.IsAvailableAsync(beerId))
        {
            return false;
        }

        _state.State.Ratings[beerId] = rating;
        await _state.WriteStateAsync();

        return true;
    }

    private long EventId => this.GetPrimaryKeyLong();
}

public class BeerRatingState
{
    public Dictionary<string, int> Ratings { get; set; } = new();
}
