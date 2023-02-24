using Orleans.Runtime;
using Orleans.Utilities;

public interface IDrinkingVenueGrain : IGrainWithIntegerCompoundKey
{
    Task<bool> IsRegisteredAsync();

    Task<VenueSummary> GetAsync(string crawlerId);

    Task RegisterAsync(Venue venue);

    Task<bool> TryCheckInAsync(string crawlerId);

    Task CheckOutAsync(string crawlerId);

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

    public Task<bool> IsRegisteredAsync()
    {
        var isRegistered = _state.State.VenueId.Length > 0;

        if (!isRegistered)
        {
            DeactivateOnIdle();
        }

        return Task.FromResult(isRegistered);
    }

    public async Task<VenueSummary> GetAsync(string crawlerId)
    {
        var eventId = this.GetPrimaryKeyLong();
        var beerRatingsGrain = GrainFactory.GetGrain<IBeerRatingGrain>(eventId, crawlerId, null);
        var ratings = await beerRatingsGrain.GetAllAsync();

        return new VenueSummary
        {
            Name = _state.State.Name,
            IsCheckedIn = _state.State.Crawlers.Contains(crawlerId),
            Beers = _state.State.Beers.ToDictionary(
                b => b,
                b => ratings.ContainsKey(b) ? ratings[b] : 0)
        };
    }

    public async Task RegisterAsync(Venue venue)
    {
        _state.State.VenueId = venue.Id;
        _state.State.Name = venue.Name;
        _state.State.Beers = new HashSet<string>(venue.Beers);

        await _state.WriteStateAsync();
    }

    public Task ObserveAsync(IHandleVenueEvents observer)
    {
        // TODO check what the parameters mean
        _observerManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public async Task<bool> TryCheckInAsync(string crawlerId)
    {
        var eventId = this.GetPrimaryKey(out var venueId);

        if (!IsRegistered)
        {
            DeactivateOnIdle();
            return false;
        }

        if (!_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Add(crawlerId);
            await _state.WriteStateAsync();

           await OnNumberOfCrawlersChangedAsync();
        }

        return true;
    }

    public async Task CheckOutAsync(string crawlerId)
    {
        if (!IsRegistered)
        {
            DeactivateOnIdle();
            return;
        }

        if (_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Remove(crawlerId);
            await _state.WriteStateAsync();

            await OnNumberOfCrawlersChangedAsync();
        }
    }

    private bool IsRegistered => _state.State.VenueId.Length > 0;

    private async Task OnNumberOfCrawlersChangedAsync()
    {
        // this.GetPrimaryKey(out var venueId);

        // _logger.LogInformation("🍻 There are now {Count} crawler(s) in {Venue}",
        //     _state.State.Crawlers.Count,
        //     venueId);

        await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(
            _state.State.VenueId, _state.State.Crawlers.Count));
    }
}

public class DrinkingVenueState
{
    public string VenueId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public HashSet<string> Beers { get; set; } = new();
    public HashSet<string> Crawlers { get; set; } = new();
}
