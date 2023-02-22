using Orleans.Runtime;
using Microsoft.AspNetCore.SignalR;
using System.Drawing;

interface IEventMapGrain : IGrainWithIntegerKey, IHandleVenueEvents
{
    Task<IEnumerable<VenueLocation>> GetAsync();

    Task AddOrUpdateVenueLocationAsync(Venue venue);
}

public class EventMapGrain : Grain, IEventMapGrain, IRemindable
{
    private readonly IPersistentState<EventMapState> _state;
    private readonly IHubContext<GeographyHub, IGeographyHub> _hubContext;
    private readonly ILogger _logger;

    public EventMapGrain(
        [PersistentState("map", "memory")] IPersistentState<EventMapState> state,
        IHubContext<GeographyHub, IGeographyHub> hubContext,
        ILogger<EventMapGrain> logger)
    {
        _state = state;
        _hubContext = hubContext;
        _logger = logger;
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
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
        };

        await _state.WriteStateAsync();

        await StartListeningAsync();
    }

    // public Task<List<VenueAttendance>> GetAttendanceAsync() =>
    //     Task.FromResult(_state.State.VenueAttendance.ToList());

    // public async Task SetVenueLocationAsync(VenueLocation venueLocation)
    // {
    //     _state.State.VenueAttendance.Add(new VenueAttendance
    //     {
    //         VenueId = venueLocation.VenueId,
    //         Name = venueLocation.Name,
    //         Latitude = venueLocation.Latitude,
    //         Longitude = venueLocation.Longitude,
    //         CrawlerCount = 0
    //     });

    //     await _state.WriteStateAsync();

    //     // TODO If system restarts, we need to auto-start listening.
    //     await StartListeningAsync();
    // }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        _logger.LogInformation("RECEIVED REMINDER");

        foreach (var venueId in _state.State.Locations.Keys)
        {
            var venueGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(EventId, venueId, null);
            await venueGrain.ObserveAsync(this.AsReference<IHandleVenueEvents>());
        }
    }

    public async Task StartListeningAsync()
    {
        _logger.LogInformation("STARTING LISTENING");

        await this.RegisterOrUpdateReminder(
            "EnsureSubscriptions",
            TimeSpan.FromSeconds(60), // TODO Check how initial due time works, doesn't seem to fire with zero or low values
            TimeSpan.FromSeconds(60));

        // TEMP TO IMMEDIATELY START LISTENING
        await ReceiveReminder("EnsureSubscriptions", default(TickStatus));
    }

    async Task IHandleVenueEvents.OnNumberOfCrawlersChangedAsync(string venueId, int crawlerCount)
    {
        _state.State.Attendance[venueId] = crawlerCount;

        await Task.WhenAll(
            _hubContext.Clients.All.OnVenueUpdated(new VenueAttendance // TODO Remove container object
            {
                Id = venueId,
                CrawlerCount = crawlerCount
            }),
            _state.WriteStateAsync());

        _logger.LogInformation("UPDATED VENUE {VenueId} WITH {CrawlerCount} CRAWLERS", venueId, crawlerCount);
    }

        private long EventId => this.GetPrimaryKeyLong();

}

public class EventMapState
{
    public Dictionary<string, VenueLocation> Locations { get; set; } = new();

    public Dictionary<string, int> Attendance { get; set; } = new();
}