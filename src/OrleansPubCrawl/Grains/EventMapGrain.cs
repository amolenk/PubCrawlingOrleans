using Orleans.Runtime;

interface IEventMapGrain : IGrainWithIntegerKey
{
    Task<IEnumerable<VenueLocation>> GetAsync();

    Task AddOrUpdateVenueLocationAsync(Venue venue);

    Task SetCrawlerCountAsync(string venueId, int count);
}

public class EventMapGrain : Grain, IEventMapGrain
{
    private readonly IPersistentState<EventMapState> _state;
    private readonly ILogger _logger;
    private string _eventId = null!;
    private IEventMapObserverListGrain _observerListGrain = null!;
    private List<IEventMapObserver> _observers = new();


    public EventMapGrain(
        [PersistentState("map")] IPersistentState<EventMapState> state,
        ILogger<EventMapGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventId = this.GetPrimaryKeyString();
        _observerListGrain = GrainFactory.GetGrain<IEventMapObserverListGrain>(Guid.Empty);

        // Set up a timer to regularly refresh the hubs, to respond to infrastructure changes.
        await RefreshObservers();
        RegisterTimer(
            _ => RefreshObservers(),
            null,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60));

        await base.OnActivateAsync(cancellationToken);
    }

    public Task<IEnumerable<VenueLocation>> GetAsync()
    {
        var locations = _state.State.Locations.Values.ToList();

        return Task.FromResult<IEnumerable<VenueLocation>>(locations);
    }

    public async Task AddOrUpdateVenueLocationAsync(Venue venue)
    {
        _state.State.Locations[venue.Id] = new VenueLocation
        {
            Id = venue.Id,
            Name = venue.Name,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
        };

        await _state.WriteStateAsync();
    }

    public async Task SetCrawlerCountAsync(string venueId, int count)
    {
        if (_state.State.Locations.TryGetValue(venueId, out var venueLocation))
        {
            venueLocation.Attendance = count;
        }

        await _state.WriteStateAsync();

        await NotifyVenueAttendanceUpdatedAsync(venueId, count);
    }

    private async Task NotifyVenueAttendanceUpdatedAsync(string venueId, int count)
    {
        await Task.WhenAll(_observers.Select(
            hub => hub.OnVenueAttendanceUpdatedAsync(_eventId, venueId, count)));
    }

    private async Task RefreshObservers()
    {
        _observers = await _observerListGrain.GetObserversAsync();
    }
}

public class EventMapState
{
    public Dictionary<string, VenueLocation> Locations { get; set; } = new();
}