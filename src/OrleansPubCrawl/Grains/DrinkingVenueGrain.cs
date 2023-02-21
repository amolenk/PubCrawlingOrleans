using Orleans.Runtime;
using Orleans.Utilities;

public interface IDrinkingVenueGrain : IGrainWithGuidCompoundKey
{
    Task RegisterAsync();

    Task RegisterBeerAsync(string beerId);

    Task EnterAsync(ICrawlerGrain crawler);

    Task LeaveAsync(ICrawlerGrain crawler);

    Task ObserveAsync(IHandleVenueEvents observer);
}

public class DrinkingVenueGrain : Grain, IDrinkingVenueGrain
{
    private readonly ObserverManager<IHandleVenueEvents> _observerManager;
    private readonly IPersistentState<DrinkingVenueState> _state;
    private readonly ILogger<DrinkingVenueGrain> _logger;

    public DrinkingVenueGrain([PersistentState("state")] IPersistentState<DrinkingVenueState> state,
        ILogger<DrinkingVenueGrain> logger)
    {
        // TODO Check that the timespan is used to clean up the observers that don't respond.
        _observerManager = new ObserverManager<IHandleVenueEvents>(TimeSpan.FromMinutes(5), logger);
        _state = state;
        _logger = logger;
    }

    public async Task RegisterAsync()
    {
        _state.State.IsRegistered = true;
        await _state.WriteStateAsync();
    }

    public async Task RegisterBeerAsync(string beerId)
    {
        var eventId = this.GetPrimaryKey(out var venueId);

        if (!_state.State.IsRegistered)
        {
            throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
        }

        var beerKey = BeerGrain.GetKey(eventId, venueId, beerId);

        var beerGrain = GrainFactory.GetGrain<IBeerGrain>(beerKey);
        await beerGrain.RegisterAsync(this);
    }

    public Task ObserveAsync(IHandleVenueEvents observer)
    {
        // TODO check what the parameters mean
        _observerManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public async Task EnterAsync(ICrawlerGrain crawler)
    {
        var eventId = this.GetPrimaryKey(out var venueId);

        if (!_state.State.IsRegistered)
        {
            throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
        }

        var crawlerId = crawler.GetPrimaryKeyString();

        if (!_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Add(crawlerId);
            await _state.WriteStateAsync();
        }

        await OnNumberOfCrawlersChangedAsync();
    }

    public async Task LeaveAsync(ICrawlerGrain crawler)
    {
        var eventId = this.GetPrimaryKey(out var venueId);

        if (!_state.State.IsRegistered)
        {
            throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
        }

        var crawlerId = crawler.GetPrimaryKeyString();

        if (_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Remove(crawlerId);
            await _state.WriteStateAsync();
        }

        await OnNumberOfCrawlersChangedAsync();
    }

    private async Task OnNumberOfCrawlersChangedAsync()
    {
        this.GetPrimaryKey(out var venueId);

        _logger.LogInformation("🍻 There are now {Count} crawler(s) in {Venue}",
            _state.State.Crawlers.Count,
            venueId);

        await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(
            venueId, _state.State.Crawlers.Count));
    }
}

public class DrinkingVenueState
{
    public bool IsRegistered { get; set; }
    public HashSet<string> Beers { get; set; } = new();
    public HashSet<string> Crawlers { get; set; } = new();
}
