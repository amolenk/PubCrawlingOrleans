using Orleans.Runtime;

public interface IDrinkingVenueGrain : IGrainWithStringKey
{
    Task CheckInAsync(ICrawlerGrain crawler);

    Task CheckOutAsync(ICrawlerGrain crawler);
}

public class DrinkingVenueGrain : Grain, IDrinkingVenueGrain
{
    private readonly IPersistentState<DrinkingVenueState> _state;
    private readonly ILogger<DrinkingVenueGrain> _logger;

    public DrinkingVenueGrain([PersistentState("venue", "venue")] IPersistentState<DrinkingVenueState> state,
        ILogger<DrinkingVenueGrain> logger)
    {
        _state = state;
        _logger = logger;
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
        }
    }
}

public class DrinkingVenueState
{
    public HashSet<string> Crawlers { get; set; } = new();
}
