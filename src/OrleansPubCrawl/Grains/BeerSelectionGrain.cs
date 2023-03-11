using Orleans.Concurrency;
using Orleans.Runtime;

public interface IBeerSelectionGrain : IGrainWithIntegerKey
{
    Task<IEnumerable<Beer>> GetAllAsync();

    Task<bool> IsAvailableAsync(string beerId);

    Task AddOrUpdateBeersAsync(IEnumerable<Beer> beers);
}

[Reentrant]
public class BeerSelectionGrain : Grain, IBeerSelectionGrain
{
    private readonly IPersistentState<BeerSelectionState> _state;
    private readonly ILogger _logger;

    public BeerSelectionGrain(
        [PersistentState("state")] IPersistentState<BeerSelectionState> state,
        ILogger<BeerSelectionGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public Task<IEnumerable<Beer>> GetAllAsync()
    {
        var beers = _state.State.Beers.Values.ToList();

        // If there are no beers, deactivate the grain.
        if (beers.Count == 0)
        {
            DeactivateOnIdle();
        }

        return Task.FromResult<IEnumerable<Beer>>(beers);
    }

    public Task<bool> IsAvailableAsync(string beerId)
    {
        var isAvailable = _state.State.Beers.ContainsKey(beerId);

        if (!isAvailable)
        {
            DeactivateOnIdle();
        }

        return Task.FromResult(isAvailable);
    }

    public async Task AddOrUpdateBeersAsync(IEnumerable<Beer> beers)
    {
        foreach (var beer in beers)
        {
            _state.State.Beers[beer.Id] = beer;
        }

        await _state.WriteStateAsync();
    }
}

public class BeerSelectionState
{
    public Dictionary<string, Beer> Beers { get; set; } = new();
}
