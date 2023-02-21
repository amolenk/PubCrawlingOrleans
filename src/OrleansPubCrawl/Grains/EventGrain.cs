using Orleans.Runtime;

public interface IEventGrain : IGrainWithGuidKey
{
    Task RegisterVenueAsync(string venueId);

    // Task<bool> IsRegisteredVenueAsync(string venueId);

    // Task<bool> ExistsAsync();
    // Task StartAsync();
}

public class EventGrain : Grain, IEventGrain
{
//    private readonly IPersistentState<EventState> _state;
    private readonly ILogger<EventGrain> _logger;

    public EventGrain(//[PersistentState("events", "memory")] IPersistentState<EventState> state,
        ILogger<EventGrain> logger)
    {
        // _state = state;
        _logger = logger;
    }

    // public Task<bool> ExistsAsync()
    // {
    //     if (_state.State.Created)
    //     {
    //         return Task.FromResult(true);
    //     }

    //     DeactivateOnIdle();
        
    //     return Task.FromResult(false);
    // }

    // public Task<bool> IsRegisteredVenueAsync(string venueId)
    // {
    //     var isRegistered = _state.State.Venues.Contains(venueId);

    //     return Task.FromResult(isRegistered);
    // }

    public async Task RegisterVenueAsync(string venueId)
    {
        // if (_state.State.Venues.Contains(venueId))
        // {
        //     return;
        // }

        var primaryKey = this.GetPrimaryKey();

        var venuGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(primaryKey, venueId, null);
        await venuGrain.RegisterAsync();

        // Is this transactional?
        var mapGrain = GrainFactory.GetGrain<IEventMapGrain>(primaryKey);

        await mapGrain.SetVenueLocationAsync(new VenueLocation
        {
            VenueId = venueId,
            Name = venueId,
            Latitude = 51.5074,
            Longitude = 0.1278
        });

        // _state.State.Venues.Add(venueId);
        // await _state.WriteStateAsync();

        _logger.LogInformation("🧱 Venue {VenueId} has joined event {EventId}",
            venueId,
            primaryKey);

        // _logger.LogInformation("🧱 Venue {VenueId} has joined {Count} other venue(s) for event {EventId}",
        //     venueId,
        //     _state.State.Venues.Count - 1,
        //     primaryKey);
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

// public class EventState
// {
//     public bool Created { get; set; }
//     public HashSet<string> Venues { get; set; } = new();
// }