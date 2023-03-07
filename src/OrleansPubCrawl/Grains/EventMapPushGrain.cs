using Orleans.Concurrency;

public interface IEventMapPushGrain : IGrainWithIntegerKey
{
    Task BroadcastAttendanceAsync(string eventId, string venueId, int attendance);
}

[Reentrant]
[StatelessWorker]
public class EventMapPushGrain : Grain, IEventMapPushGrain
{
    private List<IEventMapHubProxy> _hubs = new();

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Set up a timer to regularly refresh the hubs, to respond to infrastructure changes.
        await RefreshHubs();
        RegisterTimer(
            async _ => await RefreshHubs(),
            null,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60));

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task BroadcastAttendanceAsync(string eventId, string venueId, int attendance)
    {
        var tasks = new List<Task>(_hubs.Count);

        foreach (var hub in _hubs)
        {
            tasks.Add(hub.OnVenueAttendanceUpdatedAsync(eventId, venueId, attendance));
        }

        await Task.WhenAll(tasks);
    }

    private async ValueTask RefreshHubs()
    {
        // Discover the current infrastructure
        var hubListGrain = GrainFactory.GetGrain<IEventMapHubListGrain>(Guid.Empty);
        _hubs = await hubListGrain.GetHubsAsync();
    }
}