using Orleans.Runtime;
using Microsoft.AspNetCore.SignalR;

interface IEventMapGrain : IGrainWithGuidKey, IHandleVenueEvents
{
    Task<List<VenueAttendance>> GetAttendanceAsync();

    Task SetVenueLocationAsync(VenueLocation location);
}

public class AttendanceMapGrain : Grain, IEventMapGrain, IRemindable
{
    private readonly IPersistentState<AttendanceMapState> _state;
    private readonly IHubContext<GeographyHub, IGeographyHub> _hubContext;
    private readonly ILogger<AttendanceMapGrain> _logger;

    public AttendanceMapGrain(
        [PersistentState("map", "memory")] IPersistentState<AttendanceMapState> state,
        IHubContext<GeographyHub, IGeographyHub> hubContext,
        ILogger<AttendanceMapGrain> logger)
    {
        _state = state;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task<List<VenueAttendance>> GetAttendanceAsync() =>
        Task.FromResult(_state.State.VenueAttendance.ToList());

    public async Task SetVenueLocationAsync(VenueLocation venueLocation)
    {
        _state.State.VenueAttendance.Add(new VenueAttendance
        {
            VenueId = venueLocation.VenueId,
            Name = venueLocation.Name,
            Latitude = venueLocation.Latitude,
            Longitude = venueLocation.Longitude,
            CrawlerCount = 0
        });

        await _state.WriteStateAsync();

        // TODO If system restarts, we need to auto-start listening.
        await StartListeningAsync();
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        _logger.LogInformation("RECEIVED REMINDER");

        // foreach (var venue in _state.State.VenueAttendance)
        // {
        //     var venueGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(venue.VenueId);
        //     await venueGrain.ObserveAsync(this.AsReference<IHandleVenueEvents>());
        // }
    }

    public async Task StartListeningAsync()
    {
        _logger.LogInformation("STARTING LISTENING");

        await this.RegisterOrUpdateReminder(
            "EnsureSubscriptions",
            TimeSpan.FromSeconds(60), // TODO Check how initial due time works, doesn't seem to fire with zero or low values
            TimeSpan.FromSeconds(60));

        await ReceiveReminder("EnsureSubscriptions", default(TickStatus));
    }

    async Task IHandleVenueEvents.OnNumberOfCrawlersChangedAsync(string venueId, int crawlerCount)
    {
        // TODO Better to use a Dictionary?
        var venueAttendance = _state.State.VenueAttendance
            .FirstOrDefault(x => x.VenueId == venueId);

        if (venueAttendance is not null)
        {
            venueAttendance.CrawlerCount = crawlerCount;

            await Task.WhenAll(
                _hubContext.Clients.All.OnVenueUpdated(venueAttendance),
                _state.WriteStateAsync());
        }
    }
}

public class AttendanceMapState
{
    public HashSet<VenueAttendance> VenueAttendance { get; set; } = new();
}