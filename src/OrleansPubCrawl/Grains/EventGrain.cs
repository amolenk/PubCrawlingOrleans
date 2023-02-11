using Orleans.Runtime;

public interface IEventGrain : IGrainWithStringKey
{
    Task RegisterVenueAsync(IDrinkingVenueGrain venue);

    Task<bool> IsRegisteredVenueAsync(string venueId);

    // Task StartAsync();
}

public class EventGrain : Grain, IEventGrain
{
    private readonly IPersistentState<EventState> _state;
    private readonly ILogger<EventGrain> _logger;

    public EventGrain([PersistentState("events", "memory")] IPersistentState<EventState> state,
        ILogger<EventGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public Task<bool> IsRegisteredVenueAsync(string venueId)
    {
        var isRegistered = _state.State.Venues.Contains(venueId);

        return Task.FromResult(isRegistered);
    }

    public async Task RegisterVenueAsync(IDrinkingVenueGrain venue)
    {
        var venueId = venue.GetPrimaryKeyString();

        if (!_state.State.Venues.Contains(venueId))
        {
            _state.State.Venues.Add(venueId);
            await _state.WriteStateAsync();

            _logger.LogInformation("🧱 Venue {VenueId} has joined {Count} other venue(s) for event {EventId}",
                venueId,
                _state.State.Venues.Count - 1,
                this.GetPrimaryKeyString());

            // Is this transactional?
            var geographyGrain = GrainFactory.GetGrain<IGeographyGrain>(
                this.GetPrimaryKeyString());

            await geographyGrain.AddLocation(new Position
            {
                Latitude = 51.5074,
                Longitude = 0.1278
            }, venueId);
        }
    }

    // public async Task<bool> StartAsync()
    // {
    //     // Check if there are any venues registered
    //     if (_state.State.Venues.Count == 0)
    //     {
    //         _logger.LogWarning("🚫 Event {EventId} has no venues registered",
    //             this.GetPrimaryKeyString());

    //         return false;
    //     }

    //     // TODO Store Started state

    //     _logger.LogInformation("🍻 Event {EventId} has started with {Count} venues",
    //         this.GetPrimaryKeyString(),
    //         _state.State.Venues.Count);

    //     var geographyGrain = GrainFactory.GetGrain<IGeographyGrain>(
    //         this.GetPrimaryKeyString());
    //     await geographyGrain.StartListeningAsync();
    // }
}

public class EventState
{
    public HashSet<string> Venues { get; set; } = new();
}