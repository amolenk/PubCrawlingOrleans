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
    private IEventMapHubListGrain _hubListGrain = null!;
    private List<IEventMapHubProxy> _hubs = new();


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
        _hubListGrain = GrainFactory.GetGrain<IEventMapHubListGrain>(Guid.Empty);

        // Set up a timer to regularly refresh the hubs, to respond to infrastructure changes.
        await RefreshHubs();
        RegisterTimer(
            _ => RefreshHubs(),
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

        await SendVenueAttendanceUpdatedAsync(venueId, count);
    }

    private async Task SendVenueAttendanceUpdatedAsync(string venueId, int count)
    {
        await Task.WhenAll(_hubs.Select(
            hub => hub.SendVenueAttendanceUpdatedAsync(_eventId, venueId, count)));
    }

    private async Task RefreshHubs()
    {
        _hubs = await _hubListGrain.GetHubsAsync();
    }
}

public class EventMapState
{
    public Dictionary<string, VenueLocation> Locations { get; set; } = new();
}