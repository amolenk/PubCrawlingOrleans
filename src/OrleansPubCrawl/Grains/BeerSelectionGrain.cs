using Orleans.Runtime;
using Orleans.Utilities;

public interface IBeerSelectionGrain : IGrainWithIntegerKey
{
    Task<IEnumerable<Beer>> GetAllAsync();

    Task<bool> IsAvailableAsync(string beerId);

    Task AddOrUpdateBeersAsync(IEnumerable<Beer> beers);
}

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

            // if (_state.State.Beers.ContainsKey(beer.Id))
            // {
            //     _state.State.Beers.Remove(beer.Id);
            // }

            // _state.State.Beers.Add(beer.Id, beer);
        }

        await _state.WriteStateAsync();
    }

    // public async Task RegisterBeerAsync(string beerId)
    // {
    //     // var eventId = this.GetPrimaryKey(out var venueId);

    //     // if (!_state.State.IsRegistered)
    //     // {
    //     //     throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
    //     // }

    //     // var beerKey = BeerGrain.GetKey(eventId, venueId, beerId);

    //     // var beerGrain = GrainFactory.GetGrain<IBeerGrain>(beerKey);
    //     // await beerGrain.RegisterAsync(this);
    // }

    // public Task ObserveAsync(IHandleVenueEvents observer)
    // {
    //     // TODO check what the parameters mean
    //     _observerManager.Subscribe(observer, observer);

    //     return Task.CompletedTask;
    // }

    // public async Task EnterAsync(ICrawlerGrain crawler)
    // {
    //     var eventId = this.GetPrimaryKey(out var venueId);

    //     if (!_state.State.IsRegistered)
    //     {
    //         throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
    //     }

    //     var crawlerId = crawler.GetPrimaryKeyString();

    //     if (!_state.State.Crawlers.Contains(crawlerId))
    //     {
    //         _state.State.Crawlers.Add(crawlerId);
    //         await _state.WriteStateAsync();
    //     }

    //     await OnNumberOfCrawlersChangedAsync();
    // }

    // public async Task LeaveAsync(ICrawlerGrain crawler)
    // {
    //     var eventId = this.GetPrimaryKey(out var venueId);

    //     if (!_state.State.IsRegistered)
    //     {
    //         throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
    //     }

    //     var crawlerId = crawler.GetPrimaryKeyString();

    //     if (_state.State.Crawlers.Contains(crawlerId))
    //     {
    //         _state.State.Crawlers.Remove(crawlerId);
    //         await _state.WriteStateAsync();
    //     }

    //     await OnNumberOfCrawlersChangedAsync();
    // }

    // private async Task OnNumberOfCrawlersChangedAsync()
    // {
    //     this.GetPrimaryKey(out var venueId);

    //     _logger.LogInformation("🍻 There are now {Count} crawler(s) in {Venue}",
    //         _state.State.Crawlers.Count,
    //         venueId);

    //     await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(
    //         venueId, _state.State.Crawlers.Count));
    // }
}

public class BeerSelectionState
{
    public Dictionary<string, Beer> Beers { get; set; } = new();
}
