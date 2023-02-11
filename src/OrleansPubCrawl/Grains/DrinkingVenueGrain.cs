using Orleans.Runtime;
using Orleans.Utilities;

public interface IDrinkingVenueGrain : IGrainWithStringKey
{
    Task CheckInAsync(ICrawlerGrain crawler);

    Task CheckOutAsync(ICrawlerGrain crawler);

    Task SubscribeAsync(IHandleVenueEvents observer);
}

public class DrinkingVenueGrain : Grain, IDrinkingVenueGrain
{
    private readonly ObserverManager<IHandleVenueEvents> _observerManager;
    private readonly IPersistentState<DrinkingVenueState> _state;
    private readonly ILogger<DrinkingVenueGrain> _logger;

    public DrinkingVenueGrain([PersistentState("venues", "memory")] IPersistentState<DrinkingVenueState> state,
        ILogger<DrinkingVenueGrain> logger)
    {
        // TODO Check that the timespan is used to clean up the observers that don't respond.
        _observerManager = new ObserverManager<IHandleVenueEvents>(TimeSpan.FromMinutes(5), logger);
        _state = state;
        _logger = logger;
    }

    public Task SubscribeAsync(IHandleVenueEvents observer)
    {
        // TODO check what the parameters mean
        _observerManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public async Task CheckInAsync(ICrawlerGrain crawler)
    {
        var crawlerId = crawler.GetPrimaryKeyString();

        if (!_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Add(crawler.GetPrimaryKeyString());
            await _state.WriteStateAsync();

            _logger.LogInformation("😃 Crawler {CrawlerId} has joined {Count} other crawler(s) in {VenueId}",
                crawler.GetPrimaryKeyString(),
                _state.State.Crawlers.Count - 1,
                this.GetPrimaryKeyString());
            
            await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(_state.State.Crawlers.Count));
        }
    }

    public async Task CheckOutAsync(ICrawlerGrain crawler)
    {
        var crawlerId = crawler.GetPrimaryKeyString();

        if (_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Remove(crawler.GetPrimaryKeyString());
            await _state.WriteStateAsync();

            _logger.LogInformation("😕 Crawler {CrawlerId} has left {Count} other crawler(s) behind in {VenueId}",
                crawler.GetPrimaryKeyString(),
                _state.State.Crawlers.Count,
                this.GetPrimaryKeyString());

            // TODO Check transactional properties
            await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(_state.State.Crawlers.Count));
        }
    }
}

public class DrinkingVenueState
{
    public HashSet<string> Crawlers { get; set; } = new();
}
