using Orleans.Runtime;
using Orleans.Utilities;

public interface IDrinkingVenueGrain : IGrainWithIntegerCompoundKey
{
    Task<VenueSummary> GetSummaryAsync();

    Task RegisterAsync(Venue venue);

    Task AddCrawlerAsync(string crawlerId);

    Task RemoveCrawlerAsync(string crawlerId);

    // Task CheckInAsync(string crawlerId);

    // Task CheckOutAsync(string crawlerId);

    // Task ObserveAsync(IHandleVenueEvents observer);
}

public class DrinkingVenueGrain : Grain, IDrinkingVenueGrain
{
    // private readonly ObserverManager<IHandleVenueEvents> _observerManager;
    private readonly IPersistentState<DrinkingVenueState> _state;
    private readonly ILogger<DrinkingVenueGrain> _logger;
    private Task _reportCrawlerCountTask = Task.CompletedTask;
    private int _lastReportedCrawlerCount;

    public DrinkingVenueGrain([PersistentState("state")] IPersistentState<DrinkingVenueState> state,
        ILogger<DrinkingVenueGrain> logger)
    {
        // TODO Check that the timespan is used to clean up the observers that don't respond.
        // _observerManager = new ObserverManager<IHandleVenueEvents>(TimeSpan.FromMinutes(5), logger);
        _state = state;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Set up a timer to regularly flush.
        RegisterTimer(
            _ =>
            {
                ReportCrawlerCountAsync();
                return Task.CompletedTask;
            },
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<VenueSummary> GetSummaryAsync()//string crawlerId)
    {
//        var eventId = this.GetPrimaryKeyLong();
        // var beerRatingsGrain = GrainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
        // var ratings = await beerRatingsGrain.GetBeerRatingsAsync();

        return new VenueSummary
        {
            Name = _state.State.Name,
//            IsCheckedIn = _state.State.Crawlers.Contains(crawlerId),
            Beers = _state.State.Beers.ToList()
            // .ToDictionary(
            //     b => b,
            //     b => ratings.ContainsKey(b) ? ratings[b] : 0)
        };
    }

    public async Task RegisterAsync(Venue venue)
    {
        _state.State.VenueId = venue.Id;
        _state.State.Name = venue.Name;
        _state.State.Beers = new HashSet<string>(venue.Beers);

        await _state.WriteStateAsync();
    }

    // public Task ObserveAsync(IHandleVenueEvents observer)
    // {
    //     // TODO check what the parameters mean
    //     _observerManager.Subscribe(observer, observer);

    //     return Task.CompletedTask;
    // }

    // public async Task CheckInAsync(string crawlerId) // TODO ICrawlerGrain?
    // {
    //     var eventId = this.GetPrimaryKeyLong(out var venueId);

    //     if (!IsRegistered)
    //     {
    //         DeactivateOnIdle();
    //         throw new VenueNotAvailableException(venueId, eventId);
    //     }

    //     var crawlerGrain = GrainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
    //     await crawlerGrain.SetVenueAsync(venueId);

    //     // Update the list of crawlers in this venue.
    //     if (!_state.State.Crawlers.Contains(crawlerId))
    //     {
    //         _state.State.Crawlers.Add(crawlerId);
    //         await _state.WriteStateAsync();

    //         await OnNumberOfCrawlersChangedAsync();
    //     }
    // }

    // public async Task CheckOutAsync(string crawlerId)
    // {
    //     var eventId = this.GetPrimaryKeyLong(out var venueId);

    //     if (!IsRegistered)
    //     {
    //         DeactivateOnIdle();
    //         throw new VenueNotAvailableException(venueId, eventId);
    //     }

    //     var crawlerGrain = GrainFactory.GetGrain<ICrawlerGrain>(eventId, crawlerId, null);
    //     var currentVenueId = await crawlerGrain.GetVenueAsync();

    //     if (currentVenueId != venueId)
    //     {
    //         // Crawler is checked in somewhere else.
    //         throw new CrawlerNotCheckedInException(crawlerId, venueId, eventId);
    //     }

    //     await crawlerGrain.ClearVenueAsync();

    //     // Update the list of crawlers in this venue.
    //     if (_state.State.Crawlers.Contains(crawlerId))
    //     {
    //         _state.State.Crawlers.Remove(crawlerId);
    //         await _state.WriteStateAsync();

    //         await OnNumberOfCrawlersChangedAsync();
    //     }
    // }

    public async Task AddCrawlerAsync(string crawlerId)
    {
        var eventId = this.GetPrimaryKeyLong(out var venueId);

        if (!IsRegistered)
        {
            DeactivateOnIdle();
            throw new VenueNotAvailableException(venueId, eventId);
        }

        // Update the list of crawlers in this venue.
        if (!_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Add(crawlerId);
            await _state.WriteStateAsync();
        }
    }

    public async Task RemoveCrawlerAsync(string crawlerId)
    {
        var eventId = this.GetPrimaryKeyLong(out var venueId);

        if (!IsRegistered)
        {
            DeactivateOnIdle();
            throw new VenueNotAvailableException(venueId, eventId);
        }

        // Update the list of crawlers in this venue.
        if (_state.State.Crawlers.Contains(crawlerId))
        {
            _state.State.Crawlers.Remove(crawlerId);
            await _state.WriteStateAsync();
        }
    }

    private bool IsRegistered => _state.State.VenueId.Length > 0;

    // private async Task OnNumberOfCrawlersChangedAsync()
    // {
    //     // this.GetPrimaryKey(out var venueId);

    //     // _logger.LogInformation("🍻 There are now {Count} crawler(s) in {Venue}",
    //     //     _state.State.Crawlers.Count,
    //     //     venueId);

    //     await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(
    //         _state.State.VenueId, _state.State.Crawlers.Count));
    // }

    private Task ReportCrawlerCountAsync()
    {
        if (_reportCrawlerCountTask.IsCompleted)
        {
            _reportCrawlerCountTask = ReportCrawlerCountInternalAsync();
        }

        return _reportCrawlerCountTask;

        async Task ReportCrawlerCountInternalAsync()
        {
            var crawlerCount = _state.State.Crawlers.Count;

            if (crawlerCount != _lastReportedCrawlerCount)
            {
                var eventMapGrain = GrainFactory.GetGrain<IEventMapGrain>(this.GetPrimaryKeyLong());
                await eventMapGrain.SetCrawlerCountAsync(_state.State.VenueId, crawlerCount);

                _lastReportedCrawlerCount = crawlerCount;
            }
        }
    }
}

public class DrinkingVenueState
{
    public string VenueId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public HashSet<string> Beers { get; set; } = new();
    public HashSet<string> Crawlers { get; set; } = new();
}
