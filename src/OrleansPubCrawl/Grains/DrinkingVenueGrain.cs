using Orleans.Runtime;

public interface IDrinkingVenueGrain : IGrainWithIntegerCompoundKey
{
    Task<Venue?> GetDetailsAsync();

    Task RegisterAsync(Venue venue);

    Task AddCrawlerAsync(ICrawlerGrain crawler);

    Task RemoveCrawlerAsync(ICrawlerGrain crawler);
}

public class DrinkingVenueGrain : Grain, IDrinkingVenueGrain
{
    private readonly IPersistentState<DrinkingVenueState> _state;
    private readonly ILogger<DrinkingVenueGrain> _logger;
    private IEventMapGrain _eventMapGrain = null!;
    private int _lastReportedCrawlerCount;

    public DrinkingVenueGrain(
        [PersistentState("state")] IPersistentState<DrinkingVenueState> state,
        ILogger<DrinkingVenueGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventMapGrain = GrainFactory.GetGrain<IEventMapGrain>(this.GetPrimaryKeyLong());

        // Set up a timer to regularly flush.
        RegisterTimer(
            static async self => 
            {
                await ((DrinkingVenueGrain)self).ReportCrawlerCountAsync();
            },
            this,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));

        await base.OnActivateAsync(cancellationToken);
    }

    public Task<Venue?> GetDetailsAsync() => Task.FromResult(_state.State.Details);

    public async Task RegisterAsync(Venue venue)
    {
        var eventMapGrain = GrainFactory.GetGrain<IEventMapGrain>(this.GetPrimaryKeyLong());
        await eventMapGrain.AddOrUpdateVenueLocationAsync(venue);

        _state.State.Details = venue;
        await _state.WriteStateAsync();
    }

    public async Task AddCrawlerAsync(ICrawlerGrain crawler)
    {
        if (!IsRegistered)
        {
            DeactivateOnIdle();

            var eventId = this.GetPrimaryKeyLong(out var venueId);
            throw new VenueNotAvailableException(venueId, eventId);
        }

        // Update the list of crawlers in this venue.
        if (!_state.State.Crawlers.Contains(crawler))
        {
            _state.State.Crawlers.Add(crawler);
            await _state.WriteStateAsync();
        }
    }

    public async Task RemoveCrawlerAsync(ICrawlerGrain crawler)
    {
        var eventId = this.GetPrimaryKeyLong(out var venueId);

        if (!IsRegistered)
        {
            DeactivateOnIdle();
            throw new VenueNotAvailableException(venueId, eventId);
        }

        // Update the list of crawlers in this venue.
        if (_state.State.Crawlers.Contains(crawler))
        {
            _state.State.Crawlers.Remove(crawler);
            await _state.WriteStateAsync();
        }
    }

    private bool IsRegistered => _state.State.Details is {};

    private async Task ReportCrawlerCountAsync()
    {
        var crawlerCount = _state.State.Crawlers.Count;
        if (crawlerCount == _lastReportedCrawlerCount)
        {
            return;
        }

        await _eventMapGrain.SetCrawlerCountAsync(_state.State.Details!.Id, crawlerCount);

        _lastReportedCrawlerCount = crawlerCount;
    }
}

public class DrinkingVenueState
{
    public Venue? Details { get; set; }
    public HashSet<ICrawlerGrain> Crawlers { get; set; } = new();
}
